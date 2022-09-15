﻿// <auto-generated />
using System;
using Media.DBContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Media.DBContext.Migrations
{
    [DbContext(typeof(ApplicationDBContext))]
    [Migration("20220712122257_Added_ChecksumColumn")]
    partial class Added_ChecksumColumn
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("public")
                .HasAnnotation("ProductVersion", "6.0.6")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("GroupsRecords", b =>
                {
                    b.Property<Guid>("GroupsGroupId")
                        .HasColumnType("uuid")
                        .HasColumnName("groups_group_id");

                    b.Property<Guid>("RecordsRecordId")
                        .HasColumnType("uuid")
                        .HasColumnName("records_record_id");

                    b.HasKey("GroupsGroupId", "RecordsRecordId")
                        .HasName("pk_groups_records");

                    b.HasIndex("RecordsRecordId")
                        .HasDatabaseName("ix_groups_records_records_record_id");

                    b.ToTable("groups_records", "public");
                });

            modelBuilder.Entity("Media.DBContext.Models.Albums", b =>
                {
                    b.Property<Guid>("AlbumId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("album_id");

                    b.Property<string>("AlbumName")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("album_name");

                    b.HasKey("AlbumId")
                        .HasName("pk_albums");

                    b.ToTable("albums", "public");
                });

            modelBuilder.Entity("Media.DBContext.Models.Artists", b =>
                {
                    b.Property<Guid>("ArtistId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("artist_id");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.HasKey("ArtistId")
                        .HasName("pk_artists");

                    b.ToTable("artists", "public");
                });

            modelBuilder.Entity("Media.DBContext.Models.Genres", b =>
                {
                    b.Property<Guid>("GenreId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("genre_id");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.HasKey("GenreId")
                        .HasName("pk_genres");

                    b.ToTable("genres", "public");
                });

            modelBuilder.Entity("Media.DBContext.Models.Groups", b =>
                {
                    b.Property<Guid>("GroupId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("group_id");

                    b.Property<bool>("IsDefault")
                        .HasColumnType("boolean")
                        .HasColumnName("is_default");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.HasKey("GroupId")
                        .HasName("pk_groups");

                    b.ToTable("groups", "public");
                });

            modelBuilder.Entity("Media.DBContext.Models.Records", b =>
                {
                    b.Property<Guid>("RecordId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("record_id");

                    b.Property<Guid?>("AlbumId")
                        .HasColumnType("uuid")
                        .HasColumnName("album_id");

                    b.Property<Guid?>("ArtistId")
                        .HasColumnType("uuid")
                        .HasColumnName("artist_id");

                    b.Property<string>("Checksum")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("checksum");

                    b.Property<DateTime>("Date")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("date");

                    b.Property<string>("FilePath")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("file_path");

                    b.Property<Guid?>("GenreId")
                        .HasColumnType("uuid")
                        .HasColumnName("genre_id");

                    b.Property<string>("MimeType")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("mime_type");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("title");

                    b.Property<int>("TrackNumber")
                        .HasColumnType("integer")
                        .HasColumnName("track_number");

                    b.HasKey("RecordId")
                        .HasName("pk_records");

                    b.HasIndex("AlbumId")
                        .HasDatabaseName("ix_records_album_id");

                    b.HasIndex("ArtistId")
                        .HasDatabaseName("ix_records_artist_id");

                    b.HasIndex("GenreId")
                        .HasDatabaseName("ix_records_genre_id");

                    b.ToTable("records", "public");
                });

            modelBuilder.Entity("Media.DBContext.Models.Settings", b =>
                {
                    b.Property<Guid>("SettingId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("setting_id");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("key");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("value");

                    b.HasKey("SettingId")
                        .HasName("pk_settings");

                    b.HasIndex("Key")
                        .IsUnique()
                        .HasDatabaseName("ix_settings_key");

                    b.ToTable("settings", "public");
                });

            modelBuilder.Entity("GroupsRecords", b =>
                {
                    b.HasOne("Media.DBContext.Models.Groups", null)
                        .WithMany()
                        .HasForeignKey("GroupsGroupId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_groups_records_groups_groups_group_id");

                    b.HasOne("Media.DBContext.Models.Records", null)
                        .WithMany()
                        .HasForeignKey("RecordsRecordId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_groups_records_records_records_record_id");
                });

            modelBuilder.Entity("Media.DBContext.Models.Records", b =>
                {
                    b.HasOne("Media.DBContext.Models.Albums", "Album")
                        .WithMany("Records")
                        .HasForeignKey("AlbumId")
                        .HasConstraintName("fk_records_albums_album_id");

                    b.HasOne("Media.DBContext.Models.Artists", "Artist")
                        .WithMany("Records")
                        .HasForeignKey("ArtistId")
                        .HasConstraintName("fk_records_artists_artist_id");

                    b.HasOne("Media.DBContext.Models.Genres", "Genre")
                        .WithMany("Records")
                        .HasForeignKey("GenreId")
                        .HasConstraintName("fk_records_genres_genre_id");

                    b.Navigation("Album");

                    b.Navigation("Artist");

                    b.Navigation("Genre");
                });

            modelBuilder.Entity("Media.DBContext.Models.Albums", b =>
                {
                    b.Navigation("Records");
                });

            modelBuilder.Entity("Media.DBContext.Models.Artists", b =>
                {
                    b.Navigation("Records");
                });

            modelBuilder.Entity("Media.DBContext.Models.Genres", b =>
                {
                    b.Navigation("Records");
                });
#pragma warning restore 612, 618
        }
    }
}
