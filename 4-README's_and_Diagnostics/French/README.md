# PoE2 Route AutoSplitter

Un outil de configuration et un autosplitter LiveSplit pour le **speedrun de la campagne de Path of Exile 2**.

Version actuelle : **v3.0.0 Release Candidate**.

PoE2 Route AutoSplitter fournit des itinéraires prédéfinis et personnalisés pour :

* Exploration / complétion de zones
* Boss Rush
* Exploration + Boss Rush combinés
* Campagne Any%
* Campagne 100%
* Boss de campagne obligatoires uniquement
* Boss Pinnacle 0.5
* Temple du Chaos
* Épreuves des Sekhemas
* Itinéraires personnalisés définis par l’utilisateur
* Cartes

L’application **PoE2RouteSetup** incluse gère la majeure partie de la configuration.

Elle permet de synchroniser la pause du jeu et du chronomètre LiveSplit lorsque le menu de pause est ouvert.
L’option Game Time de LiveSplit exclut les temps de chargement et met le chronomètre en pause lorsque l’option correspondante est activée.

Captures d’écran : https://imgur.com/a/VgiRn6o

---
# Politiques de run

J’ai essayé de rendre l’outil aussi indépendant que possible des règles particulières d’un run. Les joueurs disposent donc d’une grande liberté pour choisir les règles et les déclencheurs qui leur conviennent.

Pour un nouveau départ à Riverbank, la courte période entre le réveil du personnage et la conversation avec The Wounded Man n’est volontairement pas chronométrée. Cela laisse au joueur le temps de corriger ses réglages, de sélectionner l’option « skip tutorial » ou de modifier d’autres paramètres avant le début réel du run. Après l’interaction avec The Wounded Man, le chronomètre démarre sur sa dernière réplique d’introduction.

Les démarrages par transition de zone s’activent dès que le personnage entre dans la zone prédéfinie. Pour les runs dynamiques, le chronomètre ne démarre et le suivi ne commence que lorsque le personnage entre dans cette zone précise, même si le joueur commence dans une autre zone.

En raison de la longueur du jeu, j’ai développé GameTimeWatcher, un programme simple qui demande à LiveSplit de mettre en pause son Game Time lorsque le menu Pause Game ou le menu de microtransactions est ouvert. Cette fonction permet de faire une pause ou de gérer une situation nécessitant toute l’attention du joueur. Les autres menus ne mettent pas le chronomètre en pause, car le personnage reste contrôlable. Le chronomètre continue également pendant les cinématiques en jeu puisque l’inventaire reste accessible et peut être utilisé pour optimiser le run. Actuellement, le chronomètre ne se met en pause que pendant les écrans de chargement, le menu de pause et la boutique de microtransactions.

---

# Téléchargement

Le téléchargement est disponible [ici](https://github.com/ScottHoppe414/PoE2RouteAutoSplitter/tags)

OU

Accédez à la section **Releases** de ce dépôt GitHub et téléchargez la dernière version :

**`PoE2RouteAutoSplitter-vX.X.X-Setup.exe`**

Pour la plupart des utilisateurs, l’installateur est la méthode recommandée.

Une archive ZIP portable peut également être proposée aux utilisateurs qui préfèrent ne pas utiliser l’installateur.
Dans ce cas, PowerShell doit être utilisé pour exécuter le fichier `\Setup-UI[Configuration]\Build.ps1` afin de générer `RouteSetup.exe`.

---

# Démarrage rapide

## 1. Installer PoE2 Route AutoSplitter

Exécutez :

`PoE2RouteAutoSplitter-vX.X.X-Setup.exe`

Suivez les instructions d’installation.

Après l’installation, ouvrez :

**PoE2 Route AutoSplitter**

Cela lance l’application de configuration des itinéraires.

---

## 2. Choisir votre itinéraire

L’application Setup fournit une liste d’itinéraires prédéfinis.

Sélectionnez l’itinéraire que vous souhaitez exécuter.

Exemples :

* Campagne Any%
* Campagne 100%
* Boss obligatoires uniquement
* Itinéraires d’exploration
* Itinéraires Boss Rush
* Itinéraires combinés Exploration + Boss Rush

Vous pouvez également sélectionner **Custom Route** pour créer votre propre itinéraire.

---

## 3. Générer la configuration LiveSplit

Après avoir sélectionné votre itinéraire, cliquez sur le bouton Generate.

L’application crée les fichiers nécessaires dans le répertoire :

`LiveSplit Target`

Ce dossier contient les fichiers dont LiveSplit a besoin pour l’itinéraire sélectionné.

Le contenu de **LiveSplit Target** est remplacé chaque fois qu’une nouvelle configuration est générée.

---

# Configuration de LiveSplit

Deux éléments doivent être configurés dans LiveSplit :

1. Le fichier de splits (`.lss`)
2. Le Scriptable Auto Splitter (`.asl`)

## Charger le fichier de splits

Dans le dossier **LiveSplit Target** généré, repérez le fichier `.lss`.

Ouvrez-le avec LiveSplit.

Vous pouvez également le charger manuellement dans LiveSplit via :

**File → Open Splits → From File**

Sélectionnez le fichier `.lss` généré.

---

## Ajouter le Scriptable Auto Splitter

Le script d’autosplitter doit être ajouté manuellement à votre disposition LiveSplit.

Dans LiveSplit :

1. Faites un clic droit sur LiveSplit.

2. Sélectionnez **Edit Layout**.

3. Cliquez sur le bouton **+**.

4. Sélectionnez :

   **Control → Scriptable Auto Splitter**

5. Sélectionnez le nouveau composant **Scriptable Auto Splitter**.

6. Indiquez le fichier `.asl` situé dans votre dossier **LiveSplit Target**.

7. Enregistrez votre disposition.

Vous ne devez modifier ce chemin que si vous déplacez les fichiers générés ou si vous passez à une configuration utilisant un autre fichier ASL.

> PoE2 Route AutoSplitter ne génère **pas** et ne remplace **pas** votre disposition LiveSplit.

Votre disposition reste entièrement sous votre contrôle.

---

# Configuration Boss Rush

Les itinéraires qui suivent les boss utilisent le programme **BossWatcher** inclus.

BossWatcher lit les noms des boss dans le jeu et envoie les événements de boss à l’autosplitter.

Si l’itinéraire sélectionné nécessite BossWatcher, utilisez le bouton :

**Start BossWatcher**

dans PoE2 Route Setup.

Une fenêtre de console s’ouvre.

En utilisation normale, BossWatcher n’affiche que les événements utiles, notamment :

* Boss rencontré
* Boss vaincu
* Durée du combat

Exemple :

`[21:42:18] Encountered: The Executioner`

`[21:43:07] Defeated: The Executioner | Fight time: 49.213 s`

Vous n’avez pas besoin d’interagir avec la console BossWatcher pendant le run.

Laissez-la ouverte pendant le speedrun.

---

# Itinéraires d’exploration

Les itinéraires d’exploration détectent l’entrée du personnage dans des zones précises de Path of Exile 2.

BossWatcher n’est **pas nécessaire** pour les itinéraires exclusivement basés sur l’exploration.

L’autosplitter lit automatiquement les informations de transition de zone de Path of Exile 2.

---

# Exploration + Boss Rush combinés

Les itinéraires combinés suivent à la fois :

* La complétion des zones
* Les victoires contre les boss

Pour ces itinéraires :

1. Chargez le fichier `.lss` généré.
2. Faites pointer Scriptable Auto Splitter vers le fichier `.asl` généré.
3. Démarrez BossWatcher depuis PoE2 Route Setup.
4. Commencez votre run.

Les objectifs de zones et de boss sont alors gérés par le même itinéraire.

---

# Itinéraires personnalisés

Sélectionnez **Custom Route** dans PoE2 Route Setup pour créer votre propre itinéraire.

Vous pouvez inclure :

* Des zones
* Des boss
* Des zones et des boss

Ajoutez les objectifs souhaités et placez-les dans l’ordre désiré.

Une fois terminé, générez la configuration.

L’application crée les éléments personnalisés suivants :

* `.lss`
* `.asl`
* Configuration de l’itinéraire

dans **LiveSplit Target**.

Chargez ces fichiers en suivant les mêmes instructions LiveSplit que ci-dessus.

---

# Épreuves

Destiné à l’Épreuve des Sekhemas et au Temple du Chaos.

La condition de départ correspond à la première entrée dans l’épreuve elle-même. Le foyer dans lequel vous effectuez la préparation n’est pas suivi.

Deux conditions de fin sont disponibles :

1. Vous choisissez jusqu’à quelle profondeur de l’épreuve vous souhaitez aller. Lorsque vous tuez le boss à la profondeur définie, l’épreuve se termine avec succès. Ne pas terminer l’épreuve est considéré comme un run échoué et nécessite un redémarrage manuel.

2. Quitter l’épreuve la marque comme terminée. Cette option convient aux joueurs qui souhaitent considérer la sortie de l’arène comme condition de fin. Dans ce cas, la récupération du butin, des caches, le marchand et la sélection d’Ascendance font partie du run.

---

# Ruines vaal

Le foyer est considéré comme une zone frontière pour les transitions. Ainsi, entrer dans la salle de console depuis une carte est traité comme une sortie de la carte et non comme l’entrée dans une sous-zone de cette carte.

Les Ruines vaal sont encore en développement.

---

# Cartes

La préparation d’une carte n’est pas chronométrée lorsque le joueur se trouve dans une cachette ou un autre hub de cartes. À l’entrée dans la carte, le chronomètre démarre automatiquement et un split est déclenché à la première sortie après la défaite du boss de zone. Si le joueur quitte la carte avant de vaincre le boss, le chronomètre continue. Il est donc possible de tuer rapidement le boss, de quitter la carte, puis de revenir dans la même carte pour effectuer du contenu supplémentaire avec le chronomètre en pause. (Voir la politique alternative ci-dessous.)

Les runs de cartes proposent plusieurs définitions de fin :

* Nombre fixe de cartes
* Jusqu’à la première mort (run sans mort)
* Fin manuelle
* Défaite d’un boss Pinnacle précis

Le suivi des morts propose également trois options :
* Aucun suivi des morts
* Première mort uniquement
* Suivre les morts

Si vous sélectionnez la première mort ou le suivi des morts, vous devez saisir le nom de votre personnage exactement comme il apparaît dans le jeu. Le programme lit les journaux du client pour identifier la mort de votre personnage.

Deux politiques de pause sont disponibles :

* La défaite d’un boss sert d’événement de complétion de la carte, et le split se termine à la première sortie après la défaite du boss. Ce comportement est similaire à la politique de complétion des cartes de PoE2.
* Politique alternative : le chronomètre ne se met en pause que pendant les écrans de chargement, une pause manuelle ou le menu de microtransactions (si activé). Il continue dans tous les autres cas, y compris pendant la préparation de la carte, la gestion de l’inventaire et le tri du butin.

# Changer d’itinéraire

Pour passer à un autre itinéraire :

1. Ouvrez PoE2 Route Setup.
2. Sélectionnez le nouvel itinéraire.
3. Générez de nouveau la configuration.
4. Ouvrez le nouveau fichier `.lss` dans LiveSplit.
5. Vérifiez que Scriptable Auto Splitter pointe vers le fichier `.asl` situé dans **LiveSplit Target**.
6. Démarrez BossWatcher si le nouvel itinéraire nécessite la détection des boss.

Le contenu précédent de **LiveSplit Target** est remplacé.

---

# Démarrer un run

Une fois la configuration terminée :

1. Ouvrez Path of Exile 2.
2. Ouvrez LiveSplit.
3. Chargez le fichier `.lss` de votre itinéraire.
4. Vérifiez que le composant Scriptable Auto Splitter utilise le bon fichier `.asl`.
5. Démarrez BossWatcher si votre itinéraire utilise des boss.
6. Commencez le run.

L’autosplitter gère automatiquement les objectifs configurés.

---

# Mise à jour

Lorsqu’une nouvelle version est publiée :

1. Téléchargez le dernier installateur depuis **GitHub Releases**.
2. Exécutez l’installateur.
3. Ouvrez PoE2 Route Setup.
4. Générez de nouveau votre itinéraire.

Votre disposition LiveSplit personnelle n’a pas besoin d’être remplacée.

---

# Dépannage

## Les boss ne déclenchent pas les splits

Vérifiez que :

* BossWatcher est en cours d’exécution.
* Vous avez démarré BossWatcher depuis PoE2 Route Setup.
* L’itinéraire sélectionné contient réellement des objectifs de boss.
* Le Scriptable Auto Splitter de LiveSplit pointe vers le fichier `.asl` généré.

---

## Les zones ne déclenchent pas les splits

Vérifiez que :

* Path of Exile 2 est en cours d’exécution.
* Le Scriptable Auto Splitter de LiveSplit pointe vers le bon fichier `.asl`.
* Vous avez généré le bon itinéraire d’exploration.
* Le bon fichier `.lss` est chargé.

---

## LiveSplit ouvre les mauvais splits

Ouvrez directement le fichier `.lss` depuis :

`LiveSplit Target`

ou utilisez :

**File → Open Splits → From File**

---

## J’ai changé d’itinéraire et quelque chose ne fonctionne plus

Générez de nouveau l’itinéraire et vérifiez les deux points suivants :

* Le bon fichier `.lss` est chargé.
* Scriptable Auto Splitter pointe vers le fichier `.asl` actuel dans **LiveSplit Target**.

---

## BossWatcher affiche une erreur

Fermez BossWatcher puis redémarrez-le avec le bouton **Start BossWatcher** de PoE2 Route Setup.

Si le problème persiste, joignez l’erreur affichée lorsque vous signalez le problème.

---
## BossWatcher a déclenché un split prématurément ou au moment de votre mort

BossWatcher enregistre la fin d’un combat lorsque la barre de vie du boss disparaît de l’écran. Cela peut se produire pour plusieurs raisons. Il appartient à l’utilisateur de déterminer si le split est correct. Par défaut, le programme suppose que le boss est mort et déclenche le split. Si un split se produit alors que le boss n’est pas terminé, annuler le split ramène LiveSplit à l’état précédent et vous permet de reprendre le combat avec le temps actuel. Le raccourci d’annulation de split se trouve dans les paramètres de LiveSplit.

---

# Fichiers générés pour LiveSplit

Selon l’itinéraire sélectionné, **LiveSplit Target** peut contenir :

### `.lss`

La liste des splits LiveSplit.

### `.asl`

Le script d’autosplitter utilisé par le composant Scriptable Auto Splitter de LiveSplit.

### Fichiers de route/configuration

Ils indiquent à l’autosplitter quelles zones et/ou quels boss appartiennent à l’itinéraire sélectionné.

### Fichiers d’événements de boss

Utilisés par BossWatcher et les autosplitters avec suivi des boss.

Ne modifiez pas manuellement ces fichiers à moins de savoir exactement ce que vous faites.

En utilisation normale, générez-les avec **PoE2 Route Setup**.

---

# Important

PoE2 Route AutoSplitter ne contrôle **pas** et ne remplace **pas** votre disposition LiveSplit personnelle.

Vous restez responsable de :

* L’apparence du chronomètre
* La couleur des splits
* Les polices
* La taille de la fenêtre
* Les paramètres de comparaison
* Les autres composants LiveSplit

PoE2 Route AutoSplitter fournit uniquement les splits de l’itinéraire et la configuration de l’autosplitter.

---

# Signaler un problème

Lorsque vous signalez un problème, indiquez :

* La version de PoE2 Route AutoSplitter
* L’itinéraire/le mode utilisé
* Si BossWatcher était en cours d’exécution
* Le comportement attendu
* Le comportement réellement observé
* Tout message d’erreur affiché par PoE2 Route Setup, BossWatcher ou LiveSplit

Ces informations rendent les problèmes beaucoup plus faciles à reproduire et à corriger.

---

# Vérification du package et diagnostics

Les manifestes SHA-256 permettant de vérifier les fichiers de la version ou du runtime sont stockés dans :

`3 - verification files`

Les manifestes de validation de configuration, les manifestes SHA-256 de chaque run, les journaux d’audit et les résumés lisibles des runs y sont également stockés. Ils restent en dehors de `LiveSplit Target` afin que la génération d’un nouvel itinéraire ne supprime pas les fichiers d’audit des runs précédents.

Les journaux de diagnostic de SetupUI, BossWatcher et GameTimeWatcher sont centralisés dans :

`4-README's_and_Diagnostics\Diagnostics`

Les captures PNG de diagnostic sont stockées dans :

`4-README's_and_Diagnostics\Diagnostics\images`

---

# Version majeure actuelle

**PoE2 Route AutoSplitter 3.x**

La version 3 ajoute la prise en charge multilingue de SetupUI et de la langue du jeu, les noms localisés officiels des boss et des zones lorsqu’ils sont disponibles, des politiques étendues pour la campagne, les épreuves, les Ruines vaal et les cartes, la centralisation des diagnostics et des fichiers de vérification, ainsi qu’une géométrie de capture BossWatcher adaptative basée sur la hauteur pour les clients de jeu 16:9, ultralarges et super-ultralarges.
