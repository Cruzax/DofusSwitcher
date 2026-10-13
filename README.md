# Dofus Switcher

Passe d'une fenêtre Dofus 3 à l'autre en multicompte (jusqu'à 8 persos), sans Alt+Tab.
L'outil ne fait que changer de fenêtre : il n'envoie aucune touche ni aucun clic au jeu.

## Lancer

`publish\DofusSwitcher.exe` (nécessite le runtime .NET 8 Desktop, déjà installé sur ce PC).
`DofusSwitcher.exe --settings` ouvre directement les réglages (même si l'appli tourne déjà).

## Utilisation

1. Connecte tes persos en jeu.
2. Clic droit sur la barre flottante → **Réglages** → **Ajouter un perso** → clique sur chaque perso détecté.
3. Survole un perso pour le monter / descendre / retirer. Clique sur une touche et appuie sur celle voulue
   (Suppr pour effacer). Le bouton ✕ (au survol) retire le raccourci d'un perso : il reste accessible par un
   clic sur la barre ou avec Suivant / Précédent. Par défaut : `F1`–`F4` par perso, `F5` précédent, `F6` suivant.
   Une touche en double ou déjà prise par une autre appli s'affiche en rouge.
4. **Enregistrer**.

- Clic sur une icône de la barre → affiche ce perso. Bordure dorée = perso actif, grisé = client fermé.
- La classe est lue dans le titre de la fenêtre : chaque case prend la couleur de la classe, et son
  symbole est téléchargé automatiquement depuis DofusDB (`api.dofusdb.fr/breeds`) dans
  `%APPDATA%\DofusSwitcher\classes\`. Pour utiliser ta propre image, remplace `huppermage.png`,
  `eniripsa.png`… (minuscules, sans accents) dans ce dossier (bouton dans les réglages) : l'appli ne
  réécrit jamais une image existante. Hors ligne, la case affiche les 2 premières lettres de la classe.
- Bouton pause (à côté de la poignée) : coupe les raccourcis clavier pour que les touches reviennent au
  jeu (pour écrire dans le chat par exemple). Bouton doré + icônes grisées = en pause. Une touche
  « Pause / reprise » optionnelle se règle dans les réglages ; elle reste active pendant la pause.
- Bouton mosaïque (à côté de la pause), ou la touche « Mosaïque / plein écran » des réglages (aucune par défaut) : range tous les clients en grille sur l'écran (2×2 pour 3-4 comptes, 3×2 ou 3×3 au-delà,
  côte à côte pour 2), dans l'ordre de la barre. Un second clic repasse chaque client en plein écran (sans
  bordure). Dofus doit être réglé sur **Fenêtré** (Options → Affichage → Mode d'affichage) : en « Plein écran »,
  le jeu impose sa taille et l'appli te le signale. En mosaïque, la barre des tâches Windows est cachée tant
  qu'un client est au premier plan ; elle revient sur une autre appli, en quittant la mosaïque ou l'appli.
  Après le rangement, chaque client passe un instant au premier plan : Dofus n'adopte sa nouvelle taille
  qu'à ce moment-là. Réglages → Mosaïque, vignette « Perso actif en grand » (choix par défaut) : le perso au
  premier plan prend les 3/4 de l'écran et les autres une colonne à droite, avec une transition animée à
  chaque changement de perso. Vignette « Grille » : 2×2.
  Chaque client de la mosaïque porte une étiquette (symbole de classe + pseudo, cerclée d'or pour le perso au
  premier plan), grande fenêtre comprise. Les clics traversent
  l'étiquette jusqu'au jeu ; elle se cache pendant les transitions et quand une autre appli est au premier plan.
- Bouton ⏻ au bout de la barre : ferme tous les clients Dofus (comme un clic sur leur croix). Premier clic : il
  passe au rouge ; second clic dans les 3 s : fermeture. Les clients se ferment l'un après l'autre (tous en même
  temps, ils font geler le PC).
- Initiative : dans les réglages, tape l'initiative de chaque perso (fiche de caractéristiques en jeu) dans la case
  « Init. », puis « Ranger par initiative » range l'équipe dans l'ordre des tours (la plus haute d'abord). Elle
  s'affiche en doré en haut à droite de chaque icône de la barre. Elle n'est pas lue dans le jeu : à mettre à jour
  quand l'équipement change.
- Maintenir le clic sur une icône et la glisser pour changer l'ordre (c'est aussi celui de Suivant / Précédent).
- Quand la fenêtre d'un perso clignote dans la barre des tâches (MP, invitation… selon ce que le jeu
  signale), son icône pulse en doré jusqu'à ce que tu passes dessus.
- Par défaut la barre n'apparaît que quand un client Dofus est au premier plan (désactivable dans
  Réglages → Barre flottante → « Seulement en jeu », avec l'orientation horizontale/verticale et la taille des icônes).
- Glisser la poignée `⋮⋮` pour déplacer la barre (sa position est retenue).
- L'icône près de l'horloge : Pause, Réglages, Afficher/masquer la barre, Quitter. Le clic droit sur la barre
  propose les mêmes (une barre masquée se réaffiche depuis l'icône près de l'horloge).
- Échap ferme les réglages sans enregistrer.

Config : `%APPDATA%\DofusSwitcher\config.json`. En cas de souci, les erreurs sont notées dans `errors.log`
(même dossier).

Logo : le Dofus Vulbis (`src/DofusSwitcher/Assets/`), image récupérée sur DofusDB
(`api.dofusdb.fr/img/items/23004.png`) — artwork © Ankama, usage perso.

## Développer

```bash
dotnet build src\DofusSwitcher\DofusSwitcher.csproj
dotnet publish src\DofusSwitcher\DofusSwitcher.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
```

Voir [docs/conception.md](docs/conception.md) pour l'architecture.
