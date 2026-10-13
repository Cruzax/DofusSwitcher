# Dofus Switcher — Conception

Outil Windows pour passer d'une fenêtre Dofus 3 à l'autre en multicompte (jusqu'à 8 persos),
sans enchaîner les Alt+Tab. Il ne fait **que** gérer les fenêtres (afficher, ranger, fermer) :
aucune touche ni aucun clic n'est envoyé au jeu, et rien n'est lu dans le jeu (ni mémoire, ni réseau).

## Fonctionnalités

- **Raccourci par perso** (optionnel) : une touche affiche directement la fenêtre du perso.
- **Suivant / précédent** : parcourent l'équipe dans l'ordre choisi, en sautant les clients fermés.
- **Barre flottante** : une icône par perso (couleur et symbole de la classe, touche, initiative). Perso actif en
  doré, hors ligne grisé, icône qui pulse quand la fenêtre clignote. Glisser une icône change l'ordre.
- **Initiative** : saisie à la main dans les réglages ; « Ranger par initiative » met l'équipe dans l'ordre des tours.
- **Mosaïque** : grille, ou perso actif en grand avec transition animée ; étiquette (symbole + pseudo) sur les
  fenêtres ; barre des tâches cachée tant qu'un client est au premier plan. Second clic : plein écran sans bordure.
- **Tout fermer** : ferme les clients un par un (double clic de confirmation).
- **Pause** : coupe les raccourcis pour écrire dans le chat.

Hors périmètre : lancer les clients (le launcher Ankama est obligatoire), lire l'état du jeu (initiative,
équipement, tours), envoyer des touches ou des clics au jeu.

## Détection des fenêtres

Toutes les 1,5 s, énumération des fenêtres visibles appartenant au processus `Dofus`
(nom configurable dans `ProcessNames`). Le titre Dofus 3 a la forme
`Pseudo - Classe - Version - Release` : le pseudo est la partie avant le premier ` - `.
Les titres commençant par `Dofus` (écran de connexion) sont ignorés.
Le perso actif est suivi en temps réel via `SetWinEventHook(EVENT_SYSTEM_FOREGROUND)`.

## Organisation du code

Trois couches, un dossier par sujet. Les espaces de noms suivent la couche (`DofusSwitcher.Core`,
`.Services`, `.UI`, `.Win32`), pas le sous-dossier : les sous-dossiers servent à ranger, et les fichiers XAML
gardent un seul `xmlns` par couche.

```
src/DofusSwitcher/
  App.xaml(.cs)              Démarrage, instance unique, icône près de l'horloge, journal d'erreurs
  Assets/                    Logo et icône (Dofus Vulbis)
  Core/                      Logique pure, sans Windows ni WPF : tout est testé
    Characters/              TitleParser, CharacterCycle (suivant/précédent), InitiativeOrder
    Classes/                 BreedCatalog (API DofusDB), ClassStyle (couleur et nom de fichier par classe)
    Common/                  ObservableObject
    Config/                  AppConfig, ConfigStore (config.json), ErrorLog (errors.log)
    Hotkeys/                 Hotkey ("Ctrl+F1"), HotkeyConflicts (touches en double)
    Layout/                  MosaicLayout (grille, perso actif en grand, transition), ReorderMath (glisser-déposer)
  Services/                  Ce qui parle à Windows
    Classes/                 ClassAppearanceProvider, ClassImageProvider, ClassImageDownloader
    Hotkeys/                 HotkeyManager (RegisterHotKey)
    Windows/                 WindowScanner, WindowActivator, WindowArranger, WindowAnimator, TaskbarHider,
                             ForegroundWatcher, FlashWatcher
    Switcher/                SwitcherController, découpé par thème :
                               SwitcherController.cs          persos, config, fenêtre active
                               SwitcherController.Hotkeys.cs  raccourcis, pause
                               SwitcherController.Mosaic.cs   mosaïque, transition, étiquettes, barre des tâches
                               SwitcherController.Clients.cs  actions sur tous les clients (tout fermer)
                             CharacterSlot (une icône de la barre), BarLayout, MosaicResults
  UI/
    Bar/                     FloatingBar, TileLabel (étiquette sur un client), MosaicOverlay
    Settings/                SettingsWindow ; Models/ : HotkeyField, EditableCharacter, DetectedCharacter, PreviewTile
    Controls/                ClassBadge (icône de perso réutilisée partout)
    Theme/                   Theme.xaml (couleurs et styles), Converters
  Win32/                     NativeMethods (P/Invoke user32/kernel32)
tests/DofusSwitcher.Tests/
  Core/…                     Mêmes dossiers que src/DofusSwitcher/Core
```

### Points clés

| Unité | Rôle |
|---|---|
| `WindowActivator` | Met une fenêtre au premier plan (restaure si réduite, contourne le verrou Windows via `AttachThreadInput`) |
| `WindowArranger` | Retire les bords d'une fenêtre et la place sur une case ; repère le mode « Plein écran » de Dofus (`WS_POPUP`), qui impose sa taille |
| `WindowAnimator` | Fait glisser les fenêtres (~60 images/s, `SWP_ASYNCWINDOWPOS`) puis signale la fin du redimensionnement (`WM_EXITSIZEMOVE`) |
| `TaskbarHider` | Cache / réaffiche la barre des tâches (aucun réglage Windows modifié) |
| `TileLabel` | Fenêtre transparente, sans focus, que les clics traversent (`WS_EX_TRANSPARENT`) |

## Cas particuliers

- Raccourci déjà pris par une autre appli (ou en double) : signalé, le reste fonctionne.
- Perso hors ligne : grisé, son raccourci ne fait rien, sauté par suivant/précédent.
- Deux fenêtres au même pseudo : la première trouvée est utilisée.
- Pendant l'édition des réglages, les raccourcis globaux sont suspendus pour pouvoir les saisir.
- Une seule instance de l'appli à la fois (mutex). Un second lancement avec `--settings` ouvre les réglages de la première (événement nommé).
- Erreur imprévue : notée dans `%APPDATA%\DofusSwitcher\errors.log`, l'appli continue. La barre des tâches est réaffichée même en cas de plantage.
- Suivant / précédent sur un client fermé depuis le dernier scan : on passe directement au perso suivant.
- La barre ne vole pas le focus (`WS_EX_NOACTIVATE`) et n'apparaît pas dans Alt+Tab.
- Mosaïque : Dofus reprend sa résolution peu après le retrait des bords, et n'adopte sa nouvelle taille qu'en
  reprenant la main. On replace donc les fenêtres pendant 2 s, puis chaque client passe un instant au premier plan.
- Tout fermer : les clients sont fermés un par un ; tous ensemble, ils font geler le PC.
- Plus aucun client de la mosaïque ouvert (tout fermer ou fermeture à la main) : on sort de la mosaïque, ce qui
  réaffiche la barre des tâches et retire les étiquettes.

## Tests

Tests unitaires xUnit sur toute la partie `Core/`. Raccourcis, mosaïque et fermeture : tests de bout en bout avec
de faux clients (petite appli WinForms nommée `Dofus.exe` aux titres de vrais persos) et, ponctuellement, les vrais.
