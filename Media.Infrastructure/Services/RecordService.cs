using Media.Application.Contracts.IO;
using Media.Application.Contracts.Repositories;
using Media.Application.Contracts.Services;
using Media.Application.Extensions;
using Media.Application.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;

namespace Media.Infrastructure.Services;

public class RecordService(
  IRecordRepository recordRepository,
  IArtistRepository artistRepository,
  IAlbumRepository albumRepository,
  IGenreRepository genreRepository,
  ILanguageRepository languageRepository,
  ICoverLoader coverLoader) : IRecordService
{
  public async Task DeleteRecord(Guid guid)
  {
    using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
    await RemoveRecord(guid).ConfigureAwait(false);
    scope.Complete();
  }

  public async Task DeleteFolders(IEnumerable<RecordFolder> folders)
  {
    using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
    foreach (var folder in folders)
    {
      var recordIds = recordRepository.GetRecords(folder);
      foreach (var recordId in recordIds)
      {
        await RemoveRecord(recordId).ConfigureAwait(false);
      }
    }
    scope.Complete();
  }

  public async Task SaveMetaData(RecordMetaData metaData, List<Guid> groups)
  {
    try
    {
      using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
      metaData.ArtistId = (await artistRepository.TryGetOrCreate(metaData.Artist))?.ArtistId;
      metaData.GenreId = (await genreRepository.TryGetOrCreate(metaData.Genre))?.GenreId;
      metaData.AlbumId = (await albumRepository.TryGetOrCreate(metaData.Album))?.AlbumId;
      metaData.LanguageId = (await languageRepository.TryGetOrCreate(metaData.Language))?.LanguageId;
      recordRepository.SaveMetaData(metaData, groups);
      scope.Complete();
    }
    catch (InvalidOperationException e)
    {
      if (e.InnerException is DbUpdateException)
      {
        await SaveMetaData(metaData, groups);
        return;
      }
    }
  }

  public async Task Update(Record record, byte[] cover)
  {
    using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

    var recordToUpdated = recordRepository.TryGetRecord(record.RecordId);
    if (recordToUpdated == null)
    {
      return;
    }

    // if to != null && new null remove
    // if new != null try remove old try get new and assign it

    // update artist
    var oldArtist = recordToUpdated.Artist;
    Guid? artistId = (await artistRepository.TryGetOrCreate(record.Artist))?.ArtistId;

    // update album
    var oldAlbum = recordToUpdated.Album;
    Guid? albumId = (await albumRepository.TryGetOrCreate(record.Album))?.AlbumId;

    // update genre
    var oldGenre = recordToUpdated.Genre;
    Guid? genreId = (await genreRepository.TryGetOrCreate(record.Genre))?.GenreId;

    // update language
    var oldLang = recordToUpdated.Language;
    Guid? languageId = (await languageRepository.TryGetOrCreate(record.Language))?.LanguageId;

    // update cover
    if (cover.Length > 0)
    {
      var fileName = cover.ToFileName();
      await coverLoader.Save(cover, fileName);
      record.Cover = fileName;
    }
    else
    {
      record.Cover = null;
    }

    await recordRepository.Update(record, (artistId, albumId, genreId, languageId));

    // try remove old data
    await artistRepository.TryRemove(oldArtist);
    await genreRepository.TryRemove(oldGenre);
    await albumRepository.TryRemove(oldAlbum);
    await languageRepository.TryRemove(oldLang);

    scope.Complete();
  }

  private async Task RemoveRecord(Guid id, bool removeLocked = false)
  {
    var record = recordRepository.TryGetRecord(id);
    if (record == null)
    {
      return;
    }

    if (!removeLocked && record.Locked)
    {
      return;
    }

    // Delete entry from db
    await recordRepository.RemoveRecord(id);

    // Remove artist if no record connected
    await artistRepository.TryRemove(record.Artist);

    // remove album if nor record connected
    await albumRepository.TryRemove(record.Album);

    // remove genre if no record connected
    await genreRepository.TryRemove(record.Genre);

    // remove language if no record connected
    await languageRepository.TryRemove(record.Language);
  }
}
