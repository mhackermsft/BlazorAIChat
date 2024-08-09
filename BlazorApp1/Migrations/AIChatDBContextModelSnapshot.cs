﻿// <auto-generated />
using System;
using BlazorAIChat;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace BlazorAIChat.Migrations
{
    [DbContext(typeof(AIChatDBContext))]
    partial class AIChatDBContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.7");

            modelBuilder.Entity("BlazorAIChat.Models.Config", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<int>("ExpirationDays")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("RequireAccountApprovals")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Config");
                });

            modelBuilder.Entity("BlazorAIChat.Models.User", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<string>("ApprovedBy")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DateApproved")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("DateRequested")
                        .HasColumnType("TEXT");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("Role")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });
#pragma warning restore 612, 618
        }
    }
}
