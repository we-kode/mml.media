using Media.Application.Extensions;
using Media.Application.Models;
using Media.Application.Models.Federation;
using Media.Application.Types;
using System;
using System.Linq;
using Xunit;

namespace Media.Test.Federation;

public class FederationRuleExtensionsTests
{
  private static Rule Rule(string[]? artists = null, string[]? albums = null, string[]? genres = null, int ageInDays = 0) => new()
  {
    RuleId = Guid.NewGuid(),
    Artists = [.. (artists ?? []).Select(name => new Artist { ArtistId = Guid.NewGuid(), Name = name })],
    Albums = [.. (albums ?? []).Select(name => new Album { AlbumId = Guid.NewGuid(), AlbumName = name })],
    Genres = [.. (genres ?? []).Select(name => new Genre { GenreId = Guid.NewGuid(), Name = name })],
    CreatedAt = DateTime.UtcNow.AddDays(-ageInDays),
  };

  [Theory]
  [InlineData("Artist A", "Album", "Rock", true)]
  [InlineData("  artist b ", "Album", "rock", true)]
  [InlineData("Artist C", "Album", "Rock", false)]
  [InlineData("Artist A", "Album", "Jazz", false)]
  [InlineData(null, "Album", "Rock", false)]
  public void Matches_CombinesValuesWithOrAndCriteriaWithAnd(string? artist, string album, string genre, bool expected)
  {
    var rule = Rule(artists: ["Artist A", "Artist B"], genres: ["Rock"]);

    Assert.Equal(expected, rule.Matches(artist, album, genre));
  }

  [Fact]
  public void Matches_EmptyRuleMatchesEverything()
  {
    Assert.True(Rule().Matches(null, null, null));
  }

  [Fact]
  public void MostSpecific_PrefersMoreCriteria()
  {
    var genre = Rule(genres: ["Rock"]);
    var artistAndGenre = Rule(artists: ["A"], genres: ["Rock"]);
    var all = Rule();

    Assert.Same(artistAndGenre, new[] { all, genre, artistAndGenre }.MostSpecific("A", "X", "Rock"));
  }

  [Fact]
  public void Matches_OutgoingRuleMatchesRecordsOfOneOfItsGroups()
  {
    var kids = Guid.NewGuid();
    var outgoing = Rule();
    outgoing.Direction = SyncDirection.Outgoing;
    outgoing.Groups = [new Group(kids, "Kids", false)];

    Assert.True(outgoing.Matches("A", "X", "Rock", [Guid.NewGuid(), kids]));
    Assert.False(outgoing.Matches("A", "X", "Rock", [Guid.NewGuid()]));
    Assert.False(outgoing.Matches("A", "X", "Rock"));
  }

  [Fact]
  public void Matches_GroupsOfIncomingRulesAreNoCriterion()
  {
    var incoming = Rule();
    incoming.Direction = SyncDirection.Incoming;
    incoming.Groups = [new Group(Guid.NewGuid(), "Kids", false)];

    Assert.True(incoming.Matches("A", "X", "Rock"));
  }

  [Fact]
  public void MostSpecific_PrefersRulesRestrictedToPartners()
  {
    var globalWithMoreCriteria = Rule(albums: ["X"], artists: ["A"], genres: ["Rock"]);
    var restricted = Rule(genres: ["Rock"]);
    restricted.Peers = [new Peer { PeerId = Guid.NewGuid(), Instance = "beta" }];

    Assert.Same(restricted, new[] { globalWithMoreCriteria, restricted }.MostSpecific("A", "X", "Rock"));
  }

  [Fact]
  public void MostSpecific_PrefersAlbumThenArtistThenGenreOnEqualCount()
  {
    var album = Rule(albums: ["X"]);
    var artist = Rule(artists: ["A"]);
    var genre = Rule(genres: ["Rock"]);

    Assert.Same(album, new[] { genre, artist, album }.MostSpecific("A", "X", "Rock"));
    Assert.Same(artist, new[] { genre, artist }.MostSpecific("A", "X", "Rock"));
  }

  [Fact]
  public void MostSpecific_PrefersFewerValuesThenOlderRule()
  {
    var wide = Rule(artists: ["A", "B", "C"]);
    var narrow = Rule(artists: ["A"]);
    var older = Rule(artists: ["A"], ageInDays: 3);

    Assert.Same(narrow, new[] { wide, narrow }.MostSpecific("A", null, null));
    Assert.Same(older, new[] { narrow, older }.MostSpecific("A", null, null));
  }

  [Fact]
  public void MostSpecific_ReturnsNullWithoutMatch()
  {
    Assert.Null(new[] { Rule(artists: ["A"]) }.MostSpecific("B", null, null));
    Assert.Null(Array.Empty<Rule>().MostSpecific("B", null, null));
  }
}
