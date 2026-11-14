# Casino de soirée — Blackjack à gorgées

Blackjack multijoueur en ligne où l'on ne mise pas d'argent mais des **gorgées**.
Une table s'ouvre sur un écran (PC/TV), les joueurs la rejoignent avec un **code** depuis leur téléphone.

- Connexion invité (pseudo), pas de compte.
- 3 tables ouvertes en même temps au maximum, 6 joueurs par table.
- L'écran **table** voit tout ; chaque **téléphone** ne voit que ses cartes, le croupier et ses options.
- Poker et roulette sont prévus plus tard : la structure est pensée pour les accueillir.

## Règles implémentées

| Sujet | Règle |
|---|---|
| Sabot | 4 jeux, remélangés **uniquement** quand le sabot est vide (la défausse redevient le sabot) |
| Mise | 1 à 10 gorgées, ou 0 pour passer la manche |
| Actions | Tirer, rester, doubler (2 premières cartes), séparer (jusqu'à 4 mains ; as séparés = une carte chacun) |
| Croupier | Tire jusqu'à 17, reste sur tout 17 ; s'il a blackjack, la manche se règle sans que personne joue |
| Main perdue | Le joueur **boit** sa mise |
| Main gagnée | Le joueur **distribue** sa mise aux autres joueurs, comme il veut |
| Blackjack naturel | Distribue mise × 1,5 arrondi au-dessus |
| Égalité | Rien |

Les manches s'enchaînent seules, avec des minuteurs : 20 s pour miser (à partir de la première mise ;
donne immédiate si tout le monde a répondu), 20 s par décision (sinon « rester »), 30 s pour distribuer
(sinon répartition automatique), 10 s de bilan. Tout est réglable dans
[appsettings.json](backend/src/Casino.Api/appsettings.json), sections `Tables` et `Blackjack`.

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

## Ajouter un jeu (poker, roulette)

- **Domaine** : un dossier `Casino.Domain/<Jeu>` avec son moteur, sur le modèle de `Blackjack/`.
  `Cards/` (cartes, sabot) est déjà indépendant du jeu.
- **Application** : `TableSession` porte aujourd'hui directement un `BlackjackGame`. Au deuxième jeu, extraire
  l'interface commune (avancer l'horloge, produire les vues) plutôt que de la deviner maintenant.
- **API / front** : un contrôleur et un dossier `features/<jeu>` par jeu ; tables, joueurs, jetons et temps réel sont partagés.
