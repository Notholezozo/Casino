# Grand Salon — Blackjack à gorgées

Blackjack multijoueur en ligne où l'on ne mise pas d'argent mais des **gorgées**.
Une table s'ouvre sur un écran (PC/TV), les joueurs la rejoignent avec un **code** depuis leur téléphone.

- Connexion invité (pseudo), pas de compte.
- 3 tables ouvertes en même temps au maximum, 6 joueurs par table.
- L'écran **table** voit tout ; chaque **téléphone** ne voit que ses cartes, le dealer et ses options.
- Deux jeux, à choisir à la création de la table : **blackjack** et **poker** (Texas Hold'em), sur la même
  table et avec le même croupier. La roulette est prévue plus tard.

## Règles du poker (Texas Hold'em à gorgées)

| Sujet | Règle |
|---|---|
| Jetons | Aucun : on mise des gorgées. Sans tapis, il n'y a ni all-in ni pot annexe |
| Blindes | Petite blinde = mise minimum de la table, grosse blinde = le double ; le bouton tourne à chaque main |
| Enchères | Fold, Check, Call, Raise. Une relance ajoute entre la mise minimum et la mise maximum de la table, trois relances au plus par tour d'enchères |
| Cartes | 2 cartes privées (visibles seulement sur le téléphone du joueur), 5 cartes communes : flop, turn, river. Un jeu neuf mélangé à chaque main |
| Gagnant | Meilleure main de 5 cartes parmi 7. Il **distribue tout le pot** aux autres joueurs, comme il veut ; à égalité, les gagnants se partagent le pot |
| Qui boit | Personne ne boit sa propre mise : on ne boit que ce qu'un gagnant nous donne |
| Abattage | Les mains encore en course sont montrées à tous ; si tout le monde se couche, le gagnant ne montre rien |
| Minuteurs | 25 s par décision (sinon Check si possible, Fold sinon), 30 s pour distribuer, 10 s de résultat, puis nouvelle main dès que deux joueurs sont connectés |

Un joueur déconnecté ne reçoit pas de cartes. Le cul sec n'existe pas au poker. Réglages : section `Poker`
de [appsettings.json](backend/src/Casino.Api/appsettings.json).

## Règles du blackjack

| Sujet | Règle |
|---|---|
| Sabot | 4 jeux, remélangés **uniquement** quand le sabot est vide (la défausse redevient le sabot) |
| Mise | Minimum et maximum choisis **à la création de la table** (entre 1 et 10 gorgées) ; 0 pour passer la manche |
| Cul sec | Option de la table : une mise à part, au-dessus de 10 gorgées, jamais convertie en gorgées. Perdu : tu finis ton verre. Gagné : tu désignes qui finit le sien. Doublé : 2 culs secs en jeu ; blackjack : 2 à donner |
| Actions | Hit, Stand, Double (2 premières cartes), Split (jusqu'à 4 mains ; as séparés = une carte chacun) |
| Dealer | Tire jusqu'à 17, reste sur tout 17 ; s'il a blackjack, la manche se règle sans que personne joue |
| Main perdue (Lose, Bust) | Le joueur **boit** sa mise |
| Main gagnée (Win) | Le joueur **distribue** sa mise aux autres joueurs, comme il veut |
| Blackjack naturel | Distribue mise × 1,5 arrondi au-dessus |
| Égalité (Push) | Rien |

Le vocabulaire du jeu est en anglais à l'écran (Hit, Stand, Double, Split, Bust, Push, Dealer) ; le reste
de l'interface est en français. Les libellés sont réunis dans
[blackjack-labels.ts](frontend/src/app/features/blackjack/blackjack-labels.ts).

Les manches s'enchaînent seules, avec des minuteurs : 20 s pour miser (à partir de la première mise ;
donne immédiate si tout le monde a répondu), 20 s par décision (sinon « rester »), 30 s pour distribuer
(sinon répartition automatique), 10 s de bilan. Tout est réglable dans
[appsettings.json](backend/src/Casino.Api/appsettings.json), sections `Tables` et `Blackjack`.
`Blackjack:Rules:MinBet` et `MaxBet` y servent à la fois de valeurs par défaut et de bornes pour les
mises qu'un créateur de table peut choisir.

## Interface

- **Design system « Grand Salon »** (tapis vert, or, bordeaux, ivoire ; Playfair Display et Libre Franklin) :
  les jetons sont dans [_tokens.scss](frontend/src/styles/_tokens.scss), les classes communes (boutons,
  badges, champs) dans [styles.scss](frontend/src/styles.scss). Aucun composant ne code une couleur en dur.
  Seul le thème sombre « salon » est branché.
- **Vue table sur PC** (à partir de 1024 px) : un bandeau d'informations sur 20 % de la hauteur (manche,
  phase, scores, sabot), puis la table et son dealer dans tout le reste de l'écran, sans défilement. Le code
  de la table et le QR code sont réunis dans le menu « Inviter » : ouvert tant qu'aucune manche n'a été
  jouée, il se referme ensuite, et l'hôte peut le rouvrir à tout moment.
- **Table en demi-cercle** : un dessin SVG dont la forme et la position des places sont décrites dans
  [table-geometry.ts](frontend/src/app/features/blackjack/table/table-geometry.ts). En dessous de 1024 px,
  tout s'empile et les places passent en grille.
- **Croupier 3D** : modèle procédural three.js (aucun fichier 3D ni texture), dans
  `frontend/src/app/shared/ui/dealer-3d/`. three.js n'est téléchargé qu'à l'affichage d'une table.
- **Distribution animée** : le serveur envoie toute la donne en un seul état ;
  [deal-sequencer.ts](frontend/src/app/shared/ui/dealing/deal-sequencer.ts) la remet en séquence, carte par
  carte, depuis la main du croupier. Les animations sont coupées si l'appareil demande moins de mouvement.

## Architecture

```
backend/                      .NET 10
  src/Casino.Domain           Règles pures, aucune dépendance : cartes, sabot, moteur de blackjack, entités
  src/Casino.Application      Cas d'usage : tables, joueurs, horloge, vues envoyées aux écrans, interfaces
  src/Casino.Infrastructure   EF Core + SQLite : DbContext, migrations, repositories
  src/Casino.Api              Contrôleurs REST, hub SignalR, authentification par jeton, hébergement du front
  tests/Casino.Tests          Tests du moteur et des vues
frontend/                     Angular (TypeScript, SCSS), mobile d'abord
```

Dépendances : `Api → Application → Domain` et `Infrastructure → Application`. Le domaine ne connaît ni la base ni le web.

### Comment ça circule

1. **Commandes** : le front appelle l'API REST (`/api/tables`, `/api/blackjack`). Le contrôleur ne fait que
   déléguer à un service applicatif, qui appelle le moteur.
2. **Moteur** : [BlackjackGame](backend/src/Casino.Domain/Blackjack/BlackjackGame.cs) est une machine à états
   sans horloge ni E/S — on lui passe l'instant courant. C'est ce qui le rend testable de façon déterministe.
3. **Horloge** : un service d'arrière-plan appelle `Tick` toutes les 200 ms ; c'est lui qui déclenche les minuteurs.
4. **Diffusion** : après chaque changement, une photo de la table est poussée par SignalR (`/hubs/game`) —
   une vue complète pour l'écran table, une vue filtrée par joueur
   ([BlackjackViewMapper](backend/src/Casino.Application/Blackjack/BlackjackViewMapper.cs)). La carte cachée
   du croupier et les cartes des autres ne quittent jamais le serveur.
5. **Persistance** : la partie en cours vit **en mémoire** ; SQLite garde les tables, les joueurs et
   l'historique des manches (mains, résultats, gorgées données). Un redémarrage du serveur ferme donc les
   tables ouvertes.

### Identité invité

Créer une table renvoie un jeton admin ; rejoindre renvoie un jeton joueur. Le front l'envoie en
`Authorization: Bearer` (et en `access_token` pour le WebSocket). Les jetons vivent en mémoire et
cessent d'être valables à la fermeture de la table.

## Lancer en développement

Prérequis : SDK .NET 10, Node.js, Angular CLI.

```bash
dotnet run --project backend/src/Casino.Api
```

```bash
npm --prefix frontend start
```

- Écran table : http://localhost:4200 sur le PC.
- Téléphones : `http://<ip-du-pc>:4200` sur le même Wi-Fi (le serveur Angular relaie `/api` et `/hubs` vers l'API
  sur le port 5080). Au premier lancement, Windows peut demander d'autoriser Node dans le pare-feu.

La base SQLite est créée et migrée automatiquement au démarrage dans `backend/src/Casino.Api/App_Data/casino.db`.

## Tests

```bash
dotnet test backend
```

## Mise en production sur la machine

L'API sert aussi le front compilé : un seul processus à lancer.

```bash
pwsh scripts/publish.ps1
```

```bash
dotnet publish/Casino.Api.dll --urls http://0.0.0.0:5080
```

Les joueurs ouvrent ensuite `http://<ip-de-la-machine>:5080`.

## Base de données

Après une modification des entités ou de leur configuration :

```bash
dotnet ef migrations add NomDeLaMigration --project backend/src/Casino.Infrastructure --startup-project backend/src/Casino.Api --output-dir Persistence/Migrations
```

(à lancer depuis `backend/`, après `dotnet tool restore`). La migration est appliquée au prochain démarrage.

## Ajouter un jeu (roulette…)

Le poker a été ajouté sur ce modèle ; tables, joueurs, jetons, horloge et temps réel sont partagés.

- **Domaine** : un dossier `Casino.Domain/<Jeu>` avec un moteur qui implémente
  [ITableGame](backend/src/Casino.Domain/Common/ITableGame.cs) (accueillir des joueurs, avancer avec l'horloge).
- **Application** : ses vues (`IGameViewBuilder`), son service de commandes (via `IGameCommandRunner`), et un cas
  de plus dans [GameFactory](backend/src/Casino.Application/Tables/GameFactory.cs). `TableSession` ne connaît
  aucune règle.
- **API** : un contrôleur `api/<jeu>` pour ses commandes.
- **Front** : un dossier `features/<jeu>` (pages joueur et table) et deux routes avec `canMatch`. La mise en
  page est fournie par `features/table-layout` : `TableLayout` (bandeau, table en demi-cercle, croupier) et
  `PlayerShell` (vue téléphone).

Dette connue : les types de gorgées partagés (`DrinkTransfer`, `DrinkTotals`, `RoundSummary`) et le formulaire
de distribution vivent encore dans les dossiers du blackjack, d'où le poker les importe. L'historique d'une main
de poker réutilise aussi la table des manches (le tableau à la place des cartes du dealer, le pot à la place
de son total).
