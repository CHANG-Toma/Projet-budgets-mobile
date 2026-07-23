# Projet Budget M1

Application mobile de gestion de budget (Master 1 — Ensitech), développée avec **.NET MAUI** et une base **MySQL/MariaDB**.

## Fonctionnalités

- Authentification (inscription / connexion) avec hash BCrypt
- Dashboard récapitulatif
- Gestion des transactions (revenus / dépenses)
- Budgets mensuels et catégories
- Statistiques
- Export PDF des transactions (QuestPDF)
- Profil utilisateur

## Stack

| Élément | Techno |
|--------|--------|
| Framework | .NET MAUI (net9.0) |
| Architecture | MVVM |
| BDD | MySQL / MariaDB (XAMPP) |
| Accès données | MySqlConnector |
| Sécurité | BCrypt.Net-Next |
| PDF | QuestPDF |

Plateformes ciblées : Android, iOS, Mac Catalyst, Windows.

## Prérequis

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- Visual Studio 2022 (workload **.NET Multi-platform App UI**) ou VS Code + workload MAUI
- [XAMPP](https://www.apachefriends.org/) (MySQL/MariaDB + phpMyAdmin)

## Setup base de données

1. Démarrer **Apache** et **MySQL** dans XAMPP.
2. Importer le dump SQL dans phpMyAdmin :
   - fichier recommandé : `projet_budgets (4).sql` (ou `projet_budgets.sql`)
3. Vérifier que la base `projet_budgets` est créée.

La connexion par défaut (locale XAMPP) est définie dans `Services/DbService.cs` :

```
Server=localhost;Port=3306;Database=projet_budgets;User Id=root;Password=;
```

Adapte la chaîne si ton mot de passe MySQL n’est pas vide.

## Lancer le projet

```bash
dotnet restore
dotnet build
dotnet build -t:Run -f net9.0-windows10.0.19041.0
```

Ou ouvrir `Projet Budget M1.csproj` dans Visual Studio et lancer sur Windows / Android / iOS.

## Structure

```
├── Models/          # Entités (User, Transaction, Budget, Catégorie…)
├── Views/           # Pages XAML
├── ViewModels/      # Logique MVVM
├── Services/        # DbService, PdfService
├── Converters/      # Convertisseurs XAML
├── Commands/        # RelayCommand
└── Resources/       # Styles, fonts, images
```

## Pages principales

| Route | Rôle |
|-------|------|
| Login / Register | Auth |
| DashboardPage | Vue d’ensemble |
| TransactionsPage | Liste / édition des opérations |
| BudgetPage | Budgets mensuels |
| StatisticsPage | Stats |
| ProfilePage | Profil |

## Notes

- Projet scolaire — connexion BDD en local, non prévue pour la prod.
- Le sujet détaillé est dans `Sujet_Projet_mobile.pdf`.
