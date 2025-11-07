-- Mise à jour du schéma pour supporter les décimales dans les montants
-- et ajouter l'auto-incrémentation pour id_utilisateur

USE projet_budgets;

-- Modifier la colonne montant pour accepter les décimales
ALTER TABLE depense MODIFY COLUMN montant DOUBLE NOT NULL;

-- Ajouter l'auto-incrémentation pour id_utilisateur
ALTER TABLE utilisateur MODIFY COLUMN id_utilisateur INT AUTO_INCREMENT;

-- Afficher le résultat
SELECT 'Mise à jour terminée avec succès' AS Status;

