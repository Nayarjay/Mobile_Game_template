# Gameplay

Code propre au jeu (joueur, niveaux, ennemis, HUD...). Ce dossier reste vide dans le template.

- Pas d'asmdef ici : le code compile dans Assembly-CSharp et peut utiliser DOTween et
  tous les scripts du dossier GameCore (`using GameCore;`).
- Dès que le jeu grossit, créer un `Game.asmdef` qui référence `GameCore` (compilation plus rapide).
