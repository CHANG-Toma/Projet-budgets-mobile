-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Hôte : 127.0.0.1
-- Généré le : ven. 07 nov. 2025 à 17:50
-- Version du serveur : 10.4.32-MariaDB
-- Version de PHP : 8.2.12

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Base de données : `projet_budgets`
--

-- --------------------------------------------------------

--
-- Structure de la table `attribuer`
--

CREATE TABLE `attribuer` (
  `id_categorie` int(11) NOT NULL,
  `id_budget` int(11) NOT NULL,
  `limite_categorie` double DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Déchargement des données de la table `attribuer`
--

INSERT INTO `attribuer` (`id_categorie`, `id_budget`, `limite_categorie`) VALUES
(1, 2, 200),
(2, 2, 150),
(3, 2, 850),
(6, 2, 100);

-- --------------------------------------------------------

--
-- Structure de la table `budgetmensuel`
--

CREATE TABLE `budgetmensuel` (
  `id_budget` int(11) NOT NULL,
  `mois` date NOT NULL,
  `limite` int(11) DEFAULT NULL,
  `id_utilisateur` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Déchargement des données de la table `budgetmensuel`
--

INSERT INTO `budgetmensuel` (`id_budget`, `mois`, `limite`, `id_utilisateur`) VALUES
(1, '2025-11-01', NULL, 2),
(2, '2025-11-01', NULL, 4),
(3, '2025-11-01', NULL, 4),
(4, '2025-11-01', NULL, 4),
(5, '2025-11-01', NULL, 4),
(6, '2025-11-01', NULL, 4),
(7, '2025-11-01', NULL, 4),
(8, '2025-11-01', NULL, 4),
(9, '2025-11-01', NULL, 4),
(10, '2025-11-01', NULL, 4),
(11, '2025-11-01', NULL, 4),
(12, '2025-11-01', NULL, 4);

-- --------------------------------------------------------

--
-- Structure de la table `catégorie`
--

CREATE TABLE `catégorie` (
  `id_categorie` int(11) NOT NULL,
  `nom_categorie` varchar(50) NOT NULL,
  `id_utilisateur` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Déchargement des données de la table `catégorie`
--

INSERT INTO `catégorie` (`id_categorie`, `nom_categorie`, `id_utilisateur`) VALUES
(1, 'Alimentation', 4),
(2, 'Factures', 4),
(3, 'Logement', 4),
(6, 'Transport', 4);

-- --------------------------------------------------------

--
-- Structure de la table `depense`
--

CREATE TABLE `depense` (
  `id_depense` int(11) NOT NULL,
  `date_depense` datetime NOT NULL,
  `montant` double NOT NULL,
  `description` varchar(50) DEFAULT NULL,
  `id_moyen` int(11) NOT NULL,
  `id_budget` int(11) NOT NULL,
  `id_utilisateur` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Déchargement des données de la table `depense`
--

INSERT INTO `depense` (`id_depense`, `date_depense`, `montant`, `description`, `id_moyen`, `id_budget`, `id_utilisateur`) VALUES
(2, '2025-11-07 00:00:00', -50, 'test - Alimentation', 1, 0, 2),
(4, '2025-11-07 00:00:00', 1200, 'salaire - Autres', 1, 0, 2),
(8, '2025-11-07 14:39:21', 2000, 'Salaire - Autres', 1, 0, 3),
(9, '2025-11-07 14:44:24', -100, 'test 1 - Alimentation', 1, 0, 3),
(10, '2025-10-08 00:00:00', 1600, 'test ancient mois - Autres', 1, 0, 3),
(11, '2025-11-07 00:00:00', 85, 'course auchan - Alimentation', 1, 0, 4),
(12, '2025-11-07 00:00:00', 850, 'loyer - Logement', 1, 0, 4),
(13, '2025-11-07 00:00:00', 40, 'edf - Factures', 1, 0, 4),
(14, '2025-11-07 00:00:00', 20, 'eau - Factures', 1, 0, 4),
(15, '2025-11-07 00:00:00', 50, 'free - Factures', 1, 0, 4),
(16, '2025-11-07 00:00:00', 50, 'essence - Transport', 1, 0, 4),
(17, '2025-11-07 00:00:00', 40, 'navigo - Transport', 1, 0, 4),
(18, '2025-11-07 00:00:00', 15, 'uber eats - Alimentation', 1, 0, 4),
(19, '2025-11-07 00:00:00', 20, 'resto - Alimentation', 1, 0, 4);

-- --------------------------------------------------------

--
-- Structure de la table `moyenpaiement`
--

CREATE TABLE `moyenpaiement` (
  `id_moyen` int(11) NOT NULL,
  `libelle` varchar(50) NOT NULL,
  `id_utilisateur` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Déchargement des données de la table `moyenpaiement`
--

INSERT INTO `moyenpaiement` (`id_moyen`, `libelle`, `id_utilisateur`) VALUES
(1, 'Espèces', 2);

-- --------------------------------------------------------

--
-- Structure de la table `revenu`
--

CREATE TABLE `revenu` (
  `id_revenu` int(11) NOT NULL,
  `id_utilisateur` int(11) NOT NULL,
  `mois` date NOT NULL,
  `montant` double NOT NULL,
  `libelle` varchar(100) NOT NULL DEFAULT 'Salaire',
  `date_creation` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Déchargement des données de la table `revenu`
--

INSERT INTO `revenu` (`id_revenu`, `id_utilisateur`, `mois`, `montant`, `libelle`, `date_creation`) VALUES
(1, 4, '2025-11-01', 2500, 'Salaire', '2025-11-07 17:44:44');

-- --------------------------------------------------------

--
-- Structure de la table `utilisateur`
--

CREATE TABLE `utilisateur` (
  `id_utilisateur` int(11) NOT NULL,
  `nom` varchar(50) NOT NULL,
  `prénom` varchar(50) NOT NULL,
  `email` varchar(50) NOT NULL,
  `mot_de_passe` varchar(255) NOT NULL,
  `date_inscription` datetime NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Déchargement des données de la table `utilisateur`
--

INSERT INTO `utilisateur` (`id_utilisateur`, `nom`, `prénom`, `email`, `mot_de_passe`, `date_inscription`) VALUES
(2, '', 'toma', 'toma@gmail.com', '$2a$11$bt6.RHAcqkgbwTk4dtMeIO.WK9b3db3sYa5sHyxSqdNCeNeINGCc6', '2025-11-07 14:02:52'),
(3, '', 'test', 'test@test.test', '$2a$11$VhbwsfMrZ/KSr9z58TS1/e/uQj9d4f/X3qfp14mNixgPupA/wYgQS', '2025-11-07 14:38:57'),
(4, '', 'elias', 'elias@gmail.com', '$2a$11$IJ0.tHG0Nv7Z9utBplHb1.bP.ccwwF4mRvHu8lJm303xt7Cbmcxda', '2025-11-07 17:41:43');

--
-- Index pour les tables déchargées
--

--
-- Index pour la table `attribuer`
--
ALTER TABLE `attribuer`
  ADD PRIMARY KEY (`id_categorie`,`id_budget`),
  ADD KEY `id_budget` (`id_budget`);

--
-- Index pour la table `budgetmensuel`
--
ALTER TABLE `budgetmensuel`
  ADD PRIMARY KEY (`id_budget`),
  ADD KEY `id_utilisateur` (`id_utilisateur`);

--
-- Index pour la table `catégorie`
--
ALTER TABLE `catégorie`
  ADD PRIMARY KEY (`id_categorie`),
  ADD KEY `id_utilisateur` (`id_utilisateur`);

--
-- Index pour la table `depense`
--
ALTER TABLE `depense`
  ADD PRIMARY KEY (`id_depense`),
  ADD KEY `id_moyen` (`id_moyen`),
  ADD KEY `id_budget` (`id_budget`),
  ADD KEY `id_utilisateur` (`id_utilisateur`);

--
-- Index pour la table `moyenpaiement`
--
ALTER TABLE `moyenpaiement`
  ADD PRIMARY KEY (`id_moyen`),
  ADD UNIQUE KEY `libelle` (`libelle`),
  ADD KEY `id_utilisateur` (`id_utilisateur`);

--
-- Index pour la table `revenu`
--
ALTER TABLE `revenu`
  ADD PRIMARY KEY (`id_revenu`),
  ADD KEY `id_utilisateur` (`id_utilisateur`),
  ADD KEY `mois` (`mois`);

--
-- Index pour la table `utilisateur`
--
ALTER TABLE `utilisateur`
  ADD PRIMARY KEY (`id_utilisateur`),
  ADD UNIQUE KEY `email` (`email`);

--
-- AUTO_INCREMENT pour les tables déchargées
--

--
-- AUTO_INCREMENT pour la table `budgetmensuel`
--
ALTER TABLE `budgetmensuel`
  MODIFY `id_budget` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=13;

--
-- AUTO_INCREMENT pour la table `catégorie`
--
ALTER TABLE `catégorie`
  MODIFY `id_categorie` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=7;

--
-- AUTO_INCREMENT pour la table `depense`
--
ALTER TABLE `depense`
  MODIFY `id_depense` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=20;

--
-- AUTO_INCREMENT pour la table `moyenpaiement`
--
ALTER TABLE `moyenpaiement`
  MODIFY `id_moyen` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- AUTO_INCREMENT pour la table `revenu`
--
ALTER TABLE `revenu`
  MODIFY `id_revenu` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- AUTO_INCREMENT pour la table `utilisateur`
--
ALTER TABLE `utilisateur`
  MODIFY `id_utilisateur` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=5;

--
-- Contraintes pour les tables déchargées
--

--
-- Contraintes pour la table `attribuer`
--
ALTER TABLE `attribuer`
  ADD CONSTRAINT `attribuer_ibfk_1` FOREIGN KEY (`id_categorie`) REFERENCES `catégorie` (`id_categorie`),
  ADD CONSTRAINT `attribuer_ibfk_2` FOREIGN KEY (`id_budget`) REFERENCES `budgetmensuel` (`id_budget`);

--
-- Contraintes pour la table `budgetmensuel`
--
ALTER TABLE `budgetmensuel`
  ADD CONSTRAINT `budgetmensuel_ibfk_1` FOREIGN KEY (`id_utilisateur`) REFERENCES `utilisateur` (`id_utilisateur`);

--
-- Contraintes pour la table `catégorie`
--
ALTER TABLE `catégorie`
  ADD CONSTRAINT `catégorie_ibfk_1` FOREIGN KEY (`id_utilisateur`) REFERENCES `utilisateur` (`id_utilisateur`);

--
-- Contraintes pour la table `revenu`
--
ALTER TABLE `revenu`
  ADD CONSTRAINT `revenu_ibfk_1` FOREIGN KEY (`id_utilisateur`) REFERENCES `utilisateur` (`id_utilisateur`);
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
