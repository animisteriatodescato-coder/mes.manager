-- Imposta tutte le macchine (1-11) su tutte le anime nel database
-- Questo script aggiorna il campo MacchineSuDisponibili con tutte le macchine disponibili

UPDATE Anime
SET MacchineSuDisponibili = '1;2;3;4;5;6;7;8;9;10;11'
WHERE MacchineSuDisponibili IS NULL OR MacchineSuDisponibili = '';

-- Aggiorna anche le anime che hanno già qualche macchina impostata
-- ma vogliamo sovrascrivere con tutte le macchine
UPDATE Anime
SET MacchineSuDisponibili = '1;2;3;4;5;6;7;8;9;10;11'
WHERE MacchineSuDisponibili <> '1;2;3;4;5;6;7;8;9;10;11';

-- Verifica quante anime sono state aggiornate
SELECT COUNT(*) as TotaleAnime, 
       SUM(CASE WHEN MacchineSuDisponibili = '1;2;3;4;5;6;7;8;9;10;11' THEN 1 ELSE 0 END) as AnimeConTutteLeMacchine
FROM Anime;
