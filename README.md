# 🥚 Dofus Switcher

Passe d'un perso à l'autre en multicompte sur **Dofus 3** (jusqu'à 8), sans Alt+Tab.

> 🛡️ L'appli ne fait que gérer les fenêtres : elle n'envoie **aucune touche ni aucun clic** au jeu et n'y lit rien.

## ✨ Ce qu'elle fait

- 🎯 **Barre flottante** : une icône par perso (couleur et symbole de sa classe). Un clic = ce perso au premier plan.
- ⌨️ **Raccourcis** : une touche par perso (optionnelle), plus Suivant / Précédent (`F6` / `F5` par défaut).
- 🧩 **Mosaïque** : tous tes clients à l'écran d'un clic, en grille ou avec le perso actif en grand. Un second clic
  les remet en plein écran.
- ⚡ **Initiative** : tape-la dans les réglages, puis « Ranger par initiative » met l'équipe dans l'ordre des tours.
- 🔔 **Alertes** : l'icône d'un perso pulse quand sa fenêtre clignote (MP, invitation…).
- ⏸️ **Pause** : coupe les raccourcis pour écrire dans le chat.
- ⏻ **Tout fermer** : ferme tous les clients, un par un (double clic pour confirmer).

## 🚀 Démarrer

1. Connecte tes persos en jeu.
2. Clic droit sur la barre → **Réglages** → **Ajouter un perso**.
3. Choisis tes touches, puis **Enregistrer**.

## 💡 Bon à savoir

- 🪟 Pour la mosaïque, règle Dofus sur **Fenêtré** (Options → Affichage). L'appli gère ensuite le plein écran.
- ↔️ Glisse une icône dans la barre pour changer l'ordre, et la poignée `⋮⋮` pour déplacer la barre.
- 🎨 Symboles de classe téléchargés depuis DofusDB. Pour mettre les tiens : Réglages → **Images des classes**.
- 📁 Config et journal d'erreurs : `%APPDATA%\DofusSwitcher\`.

## 🛠️ Compiler

Il faut le [SDK .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0) (Windows).

```bash
dotnet publish src\DofusSwitcher\DofusSwitcher.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
```

Puis lance `publish\DofusSwitcher.exe`. Architecture du code : [docs/conception.md](docs/conception.md).

---

<sub>Logo : le Dofus Vulbis, et symboles de classe — artworks © Ankama, via DofusDB. Projet de fan, non affilié à Ankama.</sub>
