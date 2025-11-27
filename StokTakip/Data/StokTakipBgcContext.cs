using System;
using System.Collections.Generic;
using StokTakip.Models;
using Microsoft.EntityFrameworkCore;

namespace StokTakip.Data;

public partial class StokTakipBgcContext : DbContext
{
    public StokTakipBgcContext()
    {
    }

    public StokTakipBgcContext(DbContextOptions<StokTakipBgcContext> options)
        : base(options)
    {
    }

    public virtual DbSet<KullaniciHareketi> KullaniciHarekets { get; set; }
    public virtual DbSet<AltDepo> AltDepos { get; set; }

    public virtual DbSet<Depo> Depos { get; set; }

    public virtual DbSet<DepoEslestirme> DepoEslestirmes { get; set; }

    public virtual DbSet<HareketTip> HareketTips { get; set; }

    public virtual DbSet<Kullanici> Kullanicis { get; set; }

    public virtual DbSet<KullaniciTip> KullaniciTips { get; set; }

    public virtual DbSet<OlcuBirimi> OlcuBirimis { get; set; }

    public virtual DbSet<Sorumlu> Sorumlus { get; set; }

    public virtual DbSet<Stok> Stoks { get; set; }

    public virtual DbSet<StokDurum> StokDurums { get; set; }

    public virtual DbSet<StokHareket> StokHarekets { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AltDepo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ALT_DEPO__3213E83FD27120A2");

            entity.ToTable("ALT_DEPO");

            entity.HasIndex(e => e.Id, "UQ__ALT_DEPO__3213E83E471EE332").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AltDepoAdi)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("alt_depo_adi");
            entity.Property(e => e.GuncellemeTarihi)
                .HasColumnType("datetime")
                .HasColumnName("guncelleme_tarihi");
            entity.Property(e => e.GuncelleyenKullanici).HasColumnName("guncelleyen_kullanici");
            entity.Property(e => e.OlusturanKullanici).HasColumnName("olusturan_kullanici");
            entity.Property(e => e.OlusturmaTarihi)
                .HasColumnType("datetime")
                .HasColumnName("olusturma_tarihi");
            entity.Property(e => e.Statu).HasColumnName("statu");

            entity.HasOne(d => d.GuncelleyenKullaniciNavigation).WithMany(p => p.AltDepoGuncelleyenKullaniciNavigations)
                .HasForeignKey(d => d.GuncelleyenKullanici)
                .HasConstraintName("FK__ALT_DEPO__guncel__70DDC3D8");

            entity.HasOne(d => d.OlusturanKullaniciNavigation).WithMany(p => p.AltDepoOlusturanKullaniciNavigations)
                .HasForeignKey(d => d.OlusturanKullanici)
                .HasConstraintName("FK__ALT_DEPO__olustu__6FE99F9F");
        });

        modelBuilder.Entity<Depo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DEPO__3213E83F34A43DF5");

            entity.ToTable("DEPO");

            entity.HasIndex(e => e.Id, "UQ__DEPO__3213E83ED9BAB69E").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DepoAdi)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("depo_adi");
            entity.Property(e => e.GuncellemeTarihi)
                .HasColumnType("datetime")
                .HasColumnName("guncelleme_tarihi");
            entity.Property(e => e.GuncelleyenKullanici).HasColumnName("guncelleyen_kullanici");
            entity.Property(e => e.OlusturanKullanici).HasColumnName("olusturan_kullanici");
            entity.Property(e => e.OlusturmaTarihi)
                .HasColumnType("datetime")
                .HasColumnName("olusturma_tarihi");
            entity.Property(e => e.Statu).HasColumnName("statu");

            entity.HasOne(d => d.GuncelleyenKullaniciNavigation).WithMany(p => p.DepoGuncelleyenKullaniciNavigations)
                .HasForeignKey(d => d.GuncelleyenKullanici)
                .HasConstraintName("FK__DEPO__guncelleye__6EF57B66");

            entity.HasOne(d => d.OlusturanKullaniciNavigation).WithMany(p => p.DepoOlusturanKullaniciNavigations)
                .HasForeignKey(d => d.OlusturanKullanici)
                .HasConstraintName("FK__DEPO__olusturan___6E01572D");
        });

        modelBuilder.Entity<DepoEslestirme>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DEPO_ESL__3213E83F423228D4");

            entity.ToTable("DEPO_ESLESTIRME");

            entity.HasIndex(e => e.Id, "UQ__DEPO_ESL__3213E83E2DB2643B").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AltDepoId).HasColumnName("alt_depo_id");
            entity.Property(e => e.DepoId).HasColumnName("depo_id");
            entity.Property(e => e.GuncellemeTarihi)
                .HasColumnType("datetime")
                .HasColumnName("guncelleme_tarihi");
            entity.Property(e => e.GuncelleyenKullanici).HasColumnName("guncelleyen_kullanici");
            entity.Property(e => e.OlusturanKullanici).HasColumnName("olusturan_kullanici");
            entity.Property(e => e.OlusturmaTarihi)
                .HasColumnType("datetime")
                .HasColumnName("olusturma_tarihi");
            entity.Property(e => e.Statu).HasColumnName("statu");

            entity.HasOne(d => d.AltDepo).WithMany(p => p.DepoEslestirmes)
                .HasForeignKey(d => d.AltDepoId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__DEPO_ESLE__alt_d__619B8048");

            entity.HasOne(d => d.Depo).WithMany(p => p.DepoEslestirmes)
                .HasForeignKey(d => d.DepoId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__DEPO_ESLE__depo___60A75C0F");

            entity.HasOne(d => d.GuncelleyenKullaniciNavigation).WithMany(p => p.DepoEslestirmeGuncelleyenKullaniciNavigations)
                .HasForeignKey(d => d.GuncelleyenKullanici)
                .HasConstraintName("FK__DEPO_ESLE__gunce__6383C8BA");

            entity.HasOne(d => d.OlusturanKullaniciNavigation).WithMany(p => p.DepoEslestirmeOlusturanKullaniciNavigations)
                .HasForeignKey(d => d.OlusturanKullanici)
                .HasConstraintName("FK__DEPO_ESLE__olust__628FA481");
        });

        modelBuilder.Entity<HareketTip>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__HAREKET___3213E83F9F537CEE");

            entity.ToTable("HAREKET_TIP");

            entity.HasIndex(e => e.Id, "UQ__HAREKET___3213E83EBE8D6CAC").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GuncellemeTarihi)
                .HasColumnType("datetime")
                .HasColumnName("guncelleme_tarihi");
            entity.Property(e => e.GuncelleyenKullanici).HasColumnName("guncelleyen_kullanici");
            entity.Property(e => e.HareketTipAdi)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("hareket_tip_adi");
            entity.Property(e => e.IslemGostergesi).HasColumnName("islem_gostergesi");
            entity.Property(e => e.OlusturanKullanici).HasColumnName("olusturan_kullanici");
            entity.Property(e => e.OlusturmaTarihi)
                .HasColumnType("datetime")
                .HasColumnName("olusturma_tarihi");
            entity.Property(e => e.Statu).HasColumnName("statu");

            entity.HasOne(d => d.GuncelleyenKullaniciNavigation).WithMany(p => p.HareketTipGuncelleyenKullaniciNavigations)
                .HasForeignKey(d => d.GuncelleyenKullanici)
                .HasConstraintName("FK__HAREKET_T__gunce__6B24EA82");

            entity.HasOne(d => d.OlusturanKullaniciNavigation).WithMany(p => p.HareketTipOlusturanKullaniciNavigations)
                .HasForeignKey(d => d.OlusturanKullanici)
                .HasConstraintName("FK__HAREKET_T__olust__6A30C649");
        });

        modelBuilder.Entity<Kullanici>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__KULLANIC__3213E83FBD321015");

            entity.ToTable("KULLANICI");

            entity.HasIndex(e => e.Id, "UQ__KULLANIC__3213E83EFB77F883").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GuncellemeTarihi)
                .HasColumnType("datetime")
                .HasColumnName("guncelleme_tarihi");
            entity.Property(e => e.GuncelleyenKullanici).HasColumnName("guncelleyen_kullanici");
            entity.Property(e => e.KulAd)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("kul_ad");
            entity.Property(e => e.KulSifre)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("kul_sifre");
            entity.Property(e => e.KulSoyad)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("kul_soyad");
            entity.Property(e => e.KulTip).HasColumnName("kul_tip");
            entity.Property(e => e.KulUsername)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("kul_username");
            entity.Property(e => e.OlusturanKullanici).HasColumnName("olusturan_kullanici");
            entity.Property(e => e.OlusturmaTarihi)
                .HasColumnType("datetime")
                .HasColumnName("olusturma_tarihi");
            entity.Property(e => e.Statu).HasColumnName("statu");

            entity.HasOne(d => d.GuncelleyenKullaniciNavigation).WithMany(p => p.InverseGuncelleyenKullaniciNavigation)
                .HasForeignKey(d => d.GuncelleyenKullanici)
                .HasConstraintName("FK__KULLANICI__gunce__5FB337D6");

            entity.HasOne(d => d.KulTipNavigation).WithMany(p => p.Kullanicis)
                .HasForeignKey(d => d.KulTip)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__KULLANICI__kul_t__5DCAEF64");

            entity.HasOne(d => d.OlusturanKullaniciNavigation).WithMany(p => p.InverseOlusturanKullaniciNavigation)
                .HasForeignKey(d => d.OlusturanKullanici)
                .HasConstraintName("FK__KULLANICI__olust__5EBF139D");
        });

        modelBuilder.Entity<KullaniciTip>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__KULLANIC__3213E83FD76D8C0C");

            entity.ToTable("KULLANICI_TIP");

            entity.HasIndex(e => e.Id, "UQ__KULLANIC__3213E83E63F3879A").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GuncellemeTarihi)
                .HasColumnType("datetime")
                .HasColumnName("guncelleme_tarihi");
            entity.Property(e => e.GuncelleyenKullanici).HasColumnName("guncelleyen_kullanici");
            entity.Property(e => e.KultipAdi)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("kultip_adi");
            entity.Property(e => e.OlusturanKullanici).HasColumnName("olusturan_kullanici");
            entity.Property(e => e.OlusturmaTarihi)
                .HasColumnType("datetime")
                .HasColumnName("olusturma_tarihi");
            entity.Property(e => e.Statu).HasColumnName("statu");

            entity.HasOne(d => d.GuncelleyenKullaniciNavigation).WithMany(p => p.KullaniciTipGuncelleyenKullaniciNavigations)
                .HasForeignKey(d => d.GuncelleyenKullanici)
                .HasConstraintName("FK__KULLANICI__gunce__72C60C4A");

            entity.HasOne(d => d.OlusturanKullaniciNavigation).WithMany(p => p.KullaniciTipOlusturanKullaniciNavigations)
                .HasForeignKey(d => d.OlusturanKullanici)
                .HasConstraintName("FK__KULLANICI__olust__71D1E811");
        });

        modelBuilder.Entity<OlcuBirimi>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__OLCU_BIR__3213E83F158841DA");

            entity.ToTable("OLCU_BIRIMI");

            entity.HasIndex(e => e.Id, "UQ__OLCU_BIR__3213E83EA6CA17DD").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GuncellemeTarihi)
                .HasColumnType("datetime")
                .HasColumnName("guncelleme_tarihi");
            entity.Property(e => e.GuncelleyenKullanici).HasColumnName("guncelleyen_kullanici");
            entity.Property(e => e.OlcuBirimAdi)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("olcu_birim_adi");
            entity.Property(e => e.OlusturanKullanici).HasColumnName("olusturan_kullanici");
            entity.Property(e => e.OlusturmaTarihi)
                .HasColumnType("datetime")
                .HasColumnName("olusturma_tarihi");
            entity.Property(e => e.Statu).HasColumnName("statu");

            entity.HasOne(d => d.GuncelleyenKullaniciNavigation).WithMany(p => p.OlcuBirimiGuncelleyenKullaniciNavigations)
                .HasForeignKey(d => d.GuncelleyenKullanici)
                .HasConstraintName("FK__OLCU_BIRI__gunce__6D0D32F4");

            entity.HasOne(d => d.OlusturanKullaniciNavigation).WithMany(p => p.OlcuBirimiOlusturanKullaniciNavigations)
                .HasForeignKey(d => d.OlusturanKullanici)
                .HasConstraintName("FK__OLCU_BIRI__olust__6C190EBB");
        });

        modelBuilder.Entity<Sorumlu>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SORUMLU__3213E83F32ADB919");

            entity.ToTable("SORUMLU");

            entity.HasIndex(e => e.Id, "UQ__SORUMLU__3213E83EAE92C709").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GuncellemeTarihi)
                .HasColumnType("datetime")
                .HasColumnName("guncelleme_tarihi");
            entity.Property(e => e.GuncelleyenKullanici).HasColumnName("guncelleyen_kullanici");
            entity.Property(e => e.OlusturanKullanici).HasColumnName("olusturan_kullanici");
            entity.Property(e => e.OlusturmaTarihi)
                .HasColumnType("datetime")
                .HasColumnName("olusturma_tarihi");
            entity.Property(e => e.SorumluAdi)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("sorumlu_adi");
            entity.Property(e => e.Statu).HasColumnName("statu");

            entity.HasOne(d => d.GuncelleyenKullaniciNavigation).WithMany(p => p.SorumluGuncelleyenKullaniciNavigations)
                .HasForeignKey(d => d.GuncelleyenKullanici)
                .HasConstraintName("FK__SORUMLU__guncell__693CA210");

            entity.HasOne(d => d.OlusturanKullaniciNavigation).WithMany(p => p.SorumluOlusturanKullaniciNavigations)
                .HasForeignKey(d => d.OlusturanKullanici)
                .HasConstraintName("FK__SORUMLU__olustur__68487DD7");
        });

        modelBuilder.Entity<Stok>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__STOK__3213E83F93B7F462");

            entity.ToTable("STOK");

            entity.HasIndex(e => e.Id, "UQ__STOK__3213E83EA412C15C").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GuncellemeTarihi)
                .HasColumnType("datetime")
                .HasColumnName("guncelleme_tarihi");
            entity.Property(e => e.GuncelleyenKullanici).HasColumnName("guncelleyen_kullanici");
            entity.Property(e => e.KayitMiktar)
                .HasColumnType("decimal(18, 0)")
                .HasColumnName("kayit_miktar");
            entity.Property(e => e.KayitTarihi)
                .HasColumnType("datetime")
                .HasColumnName("kayit_tarihi");
            entity.Property(e => e.OlusturanKullanici).HasColumnName("olusturan_kullanici");
            entity.Property(e => e.OlusturmaTarihi)
                .HasColumnType("datetime")
                .HasColumnName("olusturma_tarihi");
            entity.Property(e => e.Statu).HasColumnName("statu");
            entity.Property(e => e.StokAd)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("stok_ad");
            entity.Property(e => e.StokDetay)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("stok_detay");
            entity.Property(e => e.StokMarka)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("stok_marka");
            entity.Property(e => e.StokOlcuBirim).HasColumnName("stok_olcu_birim");

            entity.HasOne(d => d.GuncelleyenKullaniciNavigation).WithMany(p => p.StokGuncelleyenKullaniciNavigations)
                .HasForeignKey(d => d.GuncelleyenKullanici)
                .HasConstraintName("FK__STOK__guncelleye__5CD6CB2B");

            entity.HasOne(d => d.OlusturanKullaniciNavigation).WithMany(p => p.StokOlusturanKullaniciNavigations)
                .HasForeignKey(d => d.OlusturanKullanici)
                .HasConstraintName("FK__STOK__olusturan___5BE2A6F2");

            entity.HasOne(d => d.StokOlcuBirimNavigation).WithMany(p => p.Stoks)
                .HasForeignKey(d => d.StokOlcuBirim)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__STOK__stok_olcu___5AEE82B9");
        });

        modelBuilder.Entity<StokDurum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__STOK_DUR__3213E83F6CAC2689");

            entity.ToTable("STOK_DURUM");

            entity.HasIndex(e => e.Id, "UQ__STOK_DUR__3213E83EBE1CB2A2").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DepoEslestirmeId).HasColumnName("depo_eslestirme_id");
            entity.Property(e => e.DurumMiktar)
                .HasColumnType("decimal(18, 0)")
                .HasColumnName("durum_miktar");
            entity.Property(e => e.GuncellemeTarihi)
                .HasColumnType("datetime")
                .HasColumnName("guncelleme_tarihi");
            entity.Property(e => e.GuncelleyenKullanici).HasColumnName("guncelleyen_kullanici");
            entity.Property(e => e.OlusturanKullanici).HasColumnName("olusturan_kullanici");
            entity.Property(e => e.OlusturmaTarihi)
                .HasColumnType("datetime")
                .HasColumnName("olusturma_tarihi");
            entity.Property(e => e.StokId).HasColumnName("stok_id");

            entity.HasOne(d => d.DepoEslestirme).WithMany(p => p.StokDurums)
                .HasForeignKey(d => d.DepoEslestirmeId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__STOK_DURU__depo___656C112C");

            entity.HasOne(d => d.GuncelleyenKullaniciNavigation).WithMany(p => p.StokDurumGuncelleyenKullaniciNavigations)
                .HasForeignKey(d => d.GuncelleyenKullanici)
                .HasConstraintName("FK__STOK_DURU__gunce__6754599E");

            entity.HasOne(d => d.OlusturanKullaniciNavigation).WithMany(p => p.StokDurumOlusturanKullaniciNavigations)
                .HasForeignKey(d => d.OlusturanKullanici)
                .HasConstraintName("FK__STOK_DURU__olust__66603565");

            entity.HasOne(d => d.Stok).WithMany(p => p.StokDurums)
                .HasForeignKey(d => d.StokId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__STOK_DURU__stok___6477ECF3");
        });

        modelBuilder.Entity<StokHareket>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__STOK_HAR__3213E83F121B2B65");

            entity.ToTable("STOK_HAREKET");

            entity.HasIndex(e => e.Id, "UQ__STOK_HAR__3213E83E315093B4").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Aciklama)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("acıklama");
            entity.Property(e => e.DepoEslestirmeId).HasColumnName("depo_eslestirme_id");
            entity.Property(e => e.GuncellemeTarihi)
                .HasColumnType("datetime")
                .HasColumnName("guncelleme_tarihi");
            entity.Property(e => e.GuncelleyenKullanici).HasColumnName("guncelleyen_kullanici");
            entity.Property(e => e.HareketMiktar)
                .HasColumnType("decimal(18, 0)")
                .HasColumnName("hareket_miktar");
            entity.Property(e => e.HareketTarihi)
                .HasColumnType("datetime")
                .HasColumnName("hareket_tarihi");
            entity.Property(e => e.HareketTip).HasColumnName("hareket_tip");
            entity.Property(e => e.OlusturanKullanici).HasColumnName("olusturan_kullanici");
            entity.Property(e => e.OlusturmaTarihi)
                .HasColumnType("datetime")
                .HasColumnName("olusturma_tarihi");
            entity.Property(e => e.SorumluId).HasColumnName("sorumlu_id");
            entity.Property(e => e.StokId).HasColumnName("stok_id");

            entity.HasOne(d => d.DepoEslestirme).WithMany(p => p.StokHarekets)
                .HasForeignKey(d => d.DepoEslestirmeId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__STOK_HARE__depo___5629CD9C");

            entity.HasOne(d => d.GuncelleyenKullaniciNavigation).WithMany(p => p.StokHareketGuncelleyenKullaniciNavigations)
                .HasForeignKey(d => d.GuncelleyenKullanici)
                .HasConstraintName("FK__STOK_HARE__gunce__59FA5E80");

            entity.HasOne(d => d.HareketTipNavigation).WithMany(p => p.StokHarekets)
                .HasForeignKey(d => d.HareketTip)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__STOK_HARE__harek__571DF1D5");

            entity.HasOne(d => d.OlusturanKullaniciNavigation).WithMany(p => p.StokHareketOlusturanKullaniciNavigations)
                .HasForeignKey(d => d.OlusturanKullanici)
                .HasConstraintName("FK__STOK_HARE__olust__59063A47");

            entity.HasOne(d => d.Sorumlu).WithMany(p => p.StokHarekets)
                .HasForeignKey(d => d.SorumluId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__STOK_HARE__sorum__5812160E");

            entity.HasOne(d => d.Stok).WithMany(p => p.StokHarekets)
                .HasForeignKey(d => d.StokId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__STOK_HARE__stok___5535A963");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}