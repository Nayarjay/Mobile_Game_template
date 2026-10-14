# Mobile Game Template

Template Unity **6000.3** pour jeux mobiles (Android / iOS, portrait) : un noyau de systèmes
prêts à l'emploi (`GameCore`) et une structure de dossiers fixe, à réutiliser pour chaque jeu.

## Démarrer un nouveau jeu

1. Sur GitHub : **Use this template → Create a new repository** (privé), puis le cloner.
2. **Activer Git LFS avant le premier commit** : dans `.gitattributes`, décommenter les lignes
   `filter=lfs` (section « Binaires via Git LFS »), puis `git lfs install`.
   Le template n'utilise pas LFS (GitHub l'interdit pour un « Template repository »),
   mais un vrai jeu en a besoin dès les premiers modèles 3D et textures.
3. Ouvrir dans Unity 6000.3.9f1.
4. **Tools → Game Template → Nouveau jeu…** : nom, société, bundle id, version.
5. **Project Settings → Services** : lier un **nouveau** projet Unity Cloud (jamais celui d'un autre jeu).
6. Scène `Boot` → `GameServices` → `AdsManager` : clés LevelPlay du jeu (laisser *Ads Enabled* décoché tant qu'il n'y en a pas).
7. Écrire le jeu dans `Assets/_Project/Scripts/Gameplay` et la scène `Game`.

## Structure

```
Assets/
├─ _Project/                  tout ce qui appartient au jeu
│  ├─ Art/  Audio/  Prefabs/  Scenes/ (Boot → MainMenu → Game)
│  ├─ Settings/               InputSystem_Actions, ScriptableObjects
│  └─ Scripts/
│     ├─ GameCore/            noyau réutilisable (GameCore.asmdef, namespace GameCore)
│     │  ├─ Core/             GameBoot, GameAudio, GameSettings, GameLog, Haptics
│     │  ├─ Meta/             PlayerWallet, DailyReward, WinStreak
│     │  ├─ Monetization/     AdsManager (LevelPlay), Store (+ IStoreBackend à brancher sur Unity IAP)
│     │  ├─ Services/         PrivacyConsent (RGPD), UnityAnalyticsSink, LeaderboardManager
│     │  ├─ Notifications/    LocalNotifier (assembly séparée)
│     │  ├─ UI/               SafeAreaRoot, CanvasAspectFit, UiButtonFeedback, SceneButton, Loc, UiSpin
│     │  └─ Editor/           Nouveau jeu, scènes de base, presets de build Android
│     └─ Gameplay/            code du jeu (Assembly-CSharp)
├─ _Packs/                    packs d'assets importés (kenney_ui-pack, CC0)
├─ Plugins/                   DOTween, gradle templates Android, haptique iOS
├─ LevelPlay/, MobileDependencyResolver/   dossiers des SDK (ne pas déplacer)
└─ Resources/                 DOTweenSettings, UnityPlayerAccountSettings
Docs/                         modèles : politique de confidentialité, fiche store
```

Règles : pas d'espace dans les noms de dossiers du projet, un namespace par module,
packs de l'Asset Store importés jeu par jeu (licence : pas de dépôt public).

## Menus

- **Tools → Game Template → Nouveau jeu…**
- **Tools → Game Template → Créer les scènes de base** (recrée Boot / MainMenu / Game s'il en manque)
- **Tools → Game Template → Build → réglages TEST / STORE** (APK de test ou AAB Google Play)
- **Tools → Game Template → Boutique → Boutique simulée dans l'éditeur**

## Git

- **Template** : pas de Git LFS (règles commentées dans `.gitattributes`), pour garder le
  bouton « Use this template » de GitHub. Ne pas y ajouter de gros fichiers binaires.
- **Jeux créés depuis le template** : décommenter les règles LFS au tout début (étape 2),
  puis `git lfs install` sur chaque machine.
- Fusion des scènes et prefabs avec UnityYAMLMerge :
  `git config merge.unityyamlmerge.driver "'<Unity>/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p %O %B %A %A"`

## Checklist avant publication

- [ ] Git LFS activé dans le dépôt du jeu
- [ ] Identité : nom, bundle id, version, **version code augmenté**
- [ ] Icône (dont icône adaptative Android) et écran de démarrage
- [ ] Keystore Android (Player Settings → Publishing Settings), sauvegardé hors du dépôt
- [ ] `PrivacyConsent.PolicyUrl` rempli, `Docs/privacy-policy.md` adapté et publié en ligne
- [ ] Pubs : `AdsManager` *Ads Enabled* + clés, `PrivacyConsent.AdsLive = true` et `Version` augmentée,
      *Simulate Ads When Disabled* décoché
- [ ] Achats : `Store.Products` et identifiants créés dans Google Play / App Store
- [ ] Leaderboard : ID et environnement identiques au dashboard Unity
- [ ] **Tools → Game Template → Build → réglages STORE**, puis test sur un vrai téléphone
- [ ] Fiche store (`Docs/store-listing.md`), captures, questionnaire « Sécurité des données »
