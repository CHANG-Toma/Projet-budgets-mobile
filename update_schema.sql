-- Mise à jour du schéma pour supporter les décimales dans les montants
-- et ajouter l'auto-incrémentation pour id_utilisateur
-- et agrandir la colonne mot_de_passe pour BCrypt

USE projet_budgets;

-- Modifier la colonne montant pour accepter les décimales
ALTER TABLE depense MODIFY COLUMN montant DOUBLE NOT NULL;

-- Ajouter l'auto-incrémentation pour id_utilisateur
ALTER TABLE utilisateur MODIFY COLUMN id_utilisateur INT AUTO_INCREMENT;

-- Agrandir la colonne mot_de_passe pour supporter les hash BCrypt (60 caractères)
ALTER TABLE utilisateur MODIFY COLUMN mot_de_passe VARCHAR(255) NOT NULL;

-- Supprimer l'utilisateur de test avec le mot de passe tronqué
DELETE FROM utilisateur WHERE email = 'toma@test.com';

-- Afficher le résultat
SELECT 'Mise à jour terminée avec succès' AS Status;

