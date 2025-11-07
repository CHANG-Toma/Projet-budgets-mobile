-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Hôte : 127.0.0.1
-- Généré le : ven. 07 nov. 2025 à 15:08
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
  `id_budget` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

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
(1, '2025-11-01', NULL, 2);

-- --------------------------------------------------------

--
-- Structure de la table `catégorie`
--

CREATE TABLE `catégorie` (
  `id_categorie` int(11) NOT NULL,
  `nom_categorie` varchar(50) NOT NULL,
  `id_utilisateur` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

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
(10, '2025-10-08 00:00:00', 1600, 'test ancient mois - Autres', 1, 0, 3);

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
(3, '', 'test', 'test@test.test', '$2a$11$VhbwsfMrZ/KSr9z58TS1/e/uQj9d4f/X3qfp14mNixgPupA/wYgQS', '2025-11-07 14:38:57');

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
  MODIFY `id_budget` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- AUTO_INCREMENT pour la table `catégorie`
--
ALTER TABLE `catégorie`
  MODIFY `id_categorie` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT pour la table `depense`
--
ALTER TABLE `depense`
  MODIFY `id_depense` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=11;

--
-- AUTO_INCREMENT pour la table `moyenpaiement`
--
ALTER TABLE `moyenpaiement`
  MODIFY `id_moyen` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- AUTO_INCREMENT pour la table `utilisateur`
--
ALTER TABLE `utilisateur`
  MODIFY `id_utilisateur` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=4;

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
-- Contraintes pour la table `depense`
--
ALTER TABLE `depense`
  ADD CONSTRAINT `depense_ibfk_1` FOREIGN KEY (`id_moyen`) REFERENCES `moyenpaiement` (`id_moyen`),
  ADD CONSTRAINT `depense_ibfk_2` FOREIGN KEY (`id_budget`) REFERENCES `budgetmensuel` (`id_budget`),
  ADD CONSTRAINT `depense_ibfk_3` FOREIGN KEY (`id_utilisateur`) REFERENCES `utilisateur` (`id_utilisateur`);

--
-- Contraintes pour la table `moyenpaiement`
--
ALTER TABLE `moyenpaiement`
  ADD CONSTRAINT `moyenpaiement_ibfk_1` FOREIGN KEY (`id_utilisateur`) REFERENCES `utilisateur` (`id_utilisateur`);
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
