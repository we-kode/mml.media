using Media.Application.Constants;
using Media.Application.Contracts.IO;
using Media.Application.Contracts.Repositories;
using Media.Application.Contracts.Services;
using Media.Application.Extensions;
using Media.Application.Models;
using Media.Application.Models.Federation;
using Media.Application.Types;
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
  ICoverLoader coverLoader,
  IFederationJobRepository federationJobRepository,
  IFederationAuditLogger federationAudit) : IRecordService
{
  public async Task DeleteRecord(Guid guid, bool removeLocked = false)
  {
    var pending = new List<AuditEntry>();
    using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
    {
      await RemoveRecord(guid, pending, removeLocked).ConfigureAwait(false);
      scope.Complete();
    }
    await federationAudit.Announce(pending).ConfigureAwait(false);
  }

  public async Task DeleteFolders(IEnumerable<RecordFolder> folders)
  {
    var pending = new List<AuditEntry>();
    using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
    {
      foreach (var folder in folders)
      {
        var recordIds = recordRepository.GetRecords(folder);
        foreach (var recordId in recordIds)
        {
          await RemoveRecord(recordId, pending).ConfigureAwait(false);
        }
      }
      scope.Complete();
    }
    await federationAudit.Announce(pending).ConfigureAwait(false);
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

    // metadata of records owned by other instances are only changed by the owner, so only groups and lock are updated.
    if (recordToUpdated.OwnerInstance != Env.INSTANCE)
    {
      await recordRepository.Update(record, (null, null, null, null)).ConfigureAwait(false);
      scope.Complete();
      return;
    }

    await UpdateMetadata(recordToUpdated, record, cover, fromOwner: false).ConfigureAwait(false);
    var pending = await federationJobRepository.EnqueueForOutgoingLinks(record.RecordId, FederationJobType.UpdateRecord).ConfigureAwait(false);
    scope.Complete();
    scope.Dispose();
    await federationAudit.Announce(pending).ConfigureAwait(false);
  }

  public async Task UpdateFromOwner(Record record, byte[] cover)
  {
    using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

    var recordToUpdated = recordRepository.TryGetRecord(record.RecordId);
    if (recordToUpdated == null)
    {
      return;
    }

    record.Groups = recordToUpdated.Groups;
    record.Locked = recordToUpdated.Locked;
    await UpdateMetadata(recordToUpdated, record, cover, fromOwner: true).ConfigureAwait(false);
    scope.Complete();
  }

  private async Task UpdateMetadata(Record recordToUpdated, Record record, byte[] cover, bool fromOwner)
  {
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

    // update cover. Admins can only replace the cover, the owner also removes it.
    if (cover is { Length: > 0 })
    {
      var fileName = cover.ToFileName();
      await coverLoader.Save(cover, fileName);
      record.Cover = fileName;
    }
    else
    {
      record.Cover = fromOwner ? null : recordToUpdated.Cover;
    }

    await recordRepository.Update(record, (artistId, albumId, genreId, languageId), fromOwner);

    // try remove old data
    await artistRepository.TryRemove(oldArtist);
    await genreRepository.TryRemove(oldGenre);
    await albumRepository.TryRemove(oldAlbum);
    await languageRepository.TryRemove(oldLang);
  }

  private async Task RemoveRecord(Guid id, List<AuditEntry> pending, bool removeLocked = false)
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

    // partners the record was sent to remove it as well. The links are removed together with the record.
    if (record.OwnerInstance == Env.INSTANCE)
    {
      pending.AddRange(await federationJobRepository.EnqueueForOutgoingLinks(id, FederationJobType.DeleteRecord));
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
