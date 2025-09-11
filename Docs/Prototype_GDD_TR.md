# Prototip Spec: "Kolej Günlükleri" - Dikey Dilim (Vertical Slice)

**Belge Sürümü:** 1.0
**Tarih:** 11.09.2025
**Hazırlayan:** NAVA (Novelty-Aware Video-game Architect)

---

## 1. Prototipin Amacı ve Hedefleri

Bu prototipin temel amacı, **"Kolej Günlükleri"** oyununun temel oyun döngüsünün (Core Loop) teknik olarak mümkün ve deneyimsel olarak ilgi çekici olduğunu kanıtlamaktır. Bu bir demo değil, bir **risk azaltma aracıdır.**

### Başarı Hedefleri:
- **[Hedef 1]** Dallanan diyalog ve kişilik (Alfa/Beta) sisteminin çalıştığını ve oyuncuya anlık geri bildirim verdiğini doğrulamak.
- **[Hedef 2]** Akıllı telefon arayüzünün, oyuncu için temel bir görev yönlendirici olarak işlev gördüğünü test etmek.
- **[Hedef 3]** Bir adet temel mini-oyunun anlatı akışını bozmadan entegre edilebildiğini göstermek.
- **[Hedef 4]** 3D render karakter varlıklarının Unity içindeki sunumunun ve temel ifade değişimlerinin estetik olarak kabul edilebilir olduğunu kanıtlamak.

---

## 2. Kapsam (Scope)

Bu prototip, sadece yukarıdaki hedefleri doğrulamak için gerekli olan minimum içeriği ve özellikleri kapsar.

### Kapsam İçi (In Scope):
-   **Anlatı:** 1 adet başlangıç hikaye sahnesi (~5 dakika oynanış) ve bu sahneyi takip eden 1 adet dallanma noktası.
-   **Sistemler:**
    -   Diyalog Seçim Sistemi (Choice -> Consequence).
    -   Temel Alfa/Beta Kişilik Puanı Takibi.
    -   Akıllı Telefon UI (sadece gelen mesaj ve görev gösterme).
-   **Mekanlar:** 2 adet serbest dolaşım mekanı (Örn: "Yurt Odası", "Kampüs Bahçesi").
-   **Karakterler:** Ana Karakter (MC) + 2 adet NPC.
-   **Mini-Oyun:** 1 adet basit Ritim/QTE (Quick Time Event) oyunu.
-   **Telemetri:** `choice_made` event'inin temel entegrasyonu.

### Kapsam Dışı (Out of Scope):
-   Kaydetme/Yükleme (Save/Load) sistemi.
-   Karmaşık envanter veya ekonomi.
-   Ayarlar menüsü (Ses, Grafik vb.).
-   Karakter özelleştirme.
-   Bölüm sonu özet ekranı.
-   Tüm animasyonlar (statik render'lar ve basit geçişler kullanılacak).

---

## 3. Prototip Özellik Kırılımı (Feature Breakdown)

### 3.1. Diyalog ve Seçim Sistemi
-   **Gereksinim:** Oyuncu, sunulan 2-3 seçenekten birine tıklayabilmelidir.
-   **Teknik Detay:** Her seçim, bir `ScriptableObject` (`ChoiceData`) olarak tanımlanmalıdır. Bu obje şunları içermelidir:
    -   `string choiceText`: Butonda görünecek metin.
    -   `PersonalityImpact`: `(ALFA_Puan_Değişimi, BETA_Puan_Değişimi)` şeklinde bir struct.
    -   `NextDialogueNode`: Bu seçim yapıldığında gidilecek bir sonraki diyalog parçası.
-   **Geri Bildirim:** Seçim yapıldığında ekranda "+1 Alfa" gibi bir görsel efekt (DOTween ile basitçe scale/fade) gösterilmelidir.

### 3.2. Kişilik Sistemi (Alfa/Beta)
-   **Gereksinim:** Oyuncunun Alfa ve Beta puanlarını tutan ve güncelleyen bir yönetici (`PlayerStatsManager`) olmalıdır.
-   **Görselleştirme:** UI'da bu puanları gösteren basit bir metin alanı bulunmalıdır (Örn: "Alfa: 5 | Beta: 2").

### 3.3. Akıllı Telefon UI
-   **Gereksinim:** Hikaye belirli bir noktaya geldiğinde ekranda bir telefon simgesi belirip titremelidir. Tıklandığında, tam ekran bir mesajlaşma arayüzü açılmalıdır.
-   **İçerik:** Prototip için sadece 1 adet önceden tanımlanmış mesaj zinciri ("Kampüs bahçesine gel.") gösterilecektir.

### 3.4. Serbest Dolaşım (Free Roam)
-   **Gereksinim:** Oyuncu, 2D bir harita üzerinde tıklanabilir alanlara (`LocationTrigger`) tıklayarak mekanlar arası geçiş yapabilmelidir.
-   **Teknik Detay:** Her `LocationTrigger` tıklandığında `SceneManager.LoadSceneAsync` ile ilgili mekanın sahnesini yüklemelidir.

### 3.5. Mini-Oyun: "Ritim Dans"
-   **Gereksinim:** Belirlenen bir sahnede, oyuncudan ekranda beliren ok tuşlarına doğru zamanda basması istenen bir QTE başlatılmalıdır.
-   **İşleyiş:** 5 adımlık bir sekans. >%60 başarı, "Başarılı" sonucunu tetikler. Aksi halde "Başarısız" sonucu tetiklenir. Hikaye her iki sonuca göre de devam etmelidir.
-   **Atlama (Skip):** Ekranda bir "Atla" butonu bulunmalı ve tıklandığında mini-oyunu "Başarısız" sayarak hikayeyi devam ettirmelidir.

---

## 4. Varlık (Asset) Listesi

-   **[ ] Karakter Render'ları:**
    -   MC (İfade: Nötr)
    -   NPC_A (İfadeler: Nötr, Mutlu)
    -   NPC_B (İfadeler: Nötr, Kızgın)
-   **[ ] Mekan Arka Planları:**
    -   Yurt Odası (Gündüz)
    -   Kampüs Bahçesi (Gündüz)
-   **[ ] UI Elementleri:**
    -   Diyalog Kutusu (PNG)
    -   Seçim Butonu (Normal, Hover state)
    -   Akıllı Telefon Arayüzü (Mesajlaşma ekranı mock-up)
    -   Ok Tuşu İkonları (Yukarı, Aşağı, Sol, Sağ)
-   **[ ] Ses:**
    -   1 adet arka plan müziği (Loop)
    -   UI tıklama sesi (SFX)
    -   Mini-oyun başarı/başarısızlık sesi (SFX)

---

## 5. Teknik Gereksinimler ve Sözleşmeler

-   **State Ayrımı:** Tüm sistemler `Controller` (mantık) ve `UI` (görsel) katmanlarına ayrılmalıdır. (Örn: `DialogueManager` ve `DialogueUI_Controller`).
-   **Bağımlılık Yönetimi (Dependency Injection):** Sistemler arası referanslar için Zenject kullanılmalıdır.
-   **Prefab Sözleşmesi (Geliştirici için):**
    -   `DialogueChoice_Button.prefab`:
        -   **Public API:** `public void Initialize(ChoiceData choiceData)`
        -   **Events:** `public event Action<ChoiceData> OnChoiceSelected;`
-   **Telemetri (Data Scientist için):**
    -   `session_start(session_id, device, version)`
    -   `choice_made(scene_id, choice_id, alpha_impact, beta_impact)`

---

## 6. Test ve Başarı Kriterleri (QA Ajanı için)

### Smoke Test (3 Dakika)
1.  Uygulama açılıyor mu?
2.  Ana menüden "Yeni Oyun" başlatılabiliyor mu?
3.  İlk diyalog sahnesi yükleniyor mu?
4.  Seçim butonları tıklanabilir mi?

### Playtest Senaryosu (15 Dakika)
1.  Yeni oyun başlat.
2.  Giriş diyaloglarını oku.
3.  Sunulan ilk seçimde "Alfa" seçeneğini seç.
4.  UI'da "Alfa" puanının 1 arttığını doğrula.
5.  Diyalog bittiğinde, "Yurt Odası"nda serbest dolaşım modunun başladığını onayla.
6.  Akıllı telefon bildirimine tıkla ve mesajı oku.
7.  Haritadan "Kampüs Bahçesi"ne git.
8.  NPC_B ile olan sahneyi başlat ve Ritim mini-oyununu oyna.
9.  Mini-oyunu başarıyla tamamla ve ilgili diyalog yolunun açıldığını gör.
10. Prototip sonu ekranının göründüğünü doğrula.

### Başarı Kriterlerinin Değerlendirilmesi
-   **[Evet/Hayır]** Seçim yapmak ve sonucunu görmek tatmin edici mi?
-   **[Evet/Hayır]** Akıllı telefon, ne yapılması gerektiğini net bir şekilde iletiyor mu?
-   **[Evet/Hayır]** Mini-oyun, akışa bir engel teşkil ediyor mu, yoksa eğlenceli bir ara mı?
-   **[Evet/Hayır]** Karakterlerin görsel stili ve sunumu hedeflenen kaliteye uygun mu?