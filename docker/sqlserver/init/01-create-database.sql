USE [master];
GO

IF DB_ID(N'Roadmaps') IS NULL
BEGIN
    CREATE DATABASE [Roadmaps]
    COLLATE Latin1_General_100_CI_AI;
END;
GO

IF (
    SELECT collation_name
    FROM sys.databases
    WHERE name = N'Roadmaps'
) <> N'Latin1_General_100_CI_AI'
BEGIN
    THROW 50001,
        'Database Roadmaps has an unexpected collation. Expected Latin1_General_100_CI_AI.',
        1;
END;
GO
