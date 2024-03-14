using System;

using Dapper;

using System.Data.SQLite;

namespace RouletteDataCollector.Services
{
    public sealed class DatabaseService
    {
        private RouletteDataCollector plugin { get; init; }
        private SQLiteConnection sqconn { get; init; }

        public DatabaseService(
            RouletteDataCollector plugin,
            string databaseFilePath)
        {
            plugin.log.Debug("Start of RouletteDataCollector.DatabaseService constructor");
            this.plugin = plugin;

            plugin.log.Info($"DatabaseService DatabaseFilePath={databaseFilePath}");
            this.sqconn = new SQLiteConnection($"Data Source={databaseFilePath};Version=3;New=True;");
        }

        public void Start()
        {
            this.InitializeDatabase();
        }

        public void Stop()
        {
            this.sqconn.Close();
        }

        public static string GetTimestamp()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ff");
        }

        public void RouletteInsert(string guid, string rouletteType)
        {
            string timestamp = GetTimestamp();
            var data = new {Guid = guid, Created = timestamp, Updated = timestamp, QueueStart = timestamp, RouletteType=rouletteType};

            if (plugin.configuration.enableSaveData)
            {
                plugin.log.Info($"Roulettes INSERT {data}");
                int rowsAffected = this.sqconn.Execute("INSERT INTO Roulettes (id, created, updated, queue_start, roulette_type) VALUES (@Guid, @Created, @Updated, @QueueStart, @RouletteType)", data);
                if (rowsAffected != 1)
                {
                    this.plugin.log.Warning($"Roulettes INSERT affected {rowsAffected} rows instead of 1");
                }
            }
        }

        public void StartLocationUpdate(string guid, uint territoryId, uint contentId)
        {
            string timestamp = GetTimestamp();
            if (plugin.configuration.enableSaveData)
            {
                plugin.log.Info($"Start location UPDATE {guid} updated = '{timestamp}', queue_end = '{timestamp}', duty_start = '{timestamp}', territory_id = {territoryId}, content_id = {contentId}");
                int rowsAffected = this.sqconn.Execute($@"
                    UPDATE Roulettes
                    SET updated = '{timestamp}', queue_end = '{timestamp}', duty_start = '{timestamp}', territory_id = {territoryId}, content_id = {contentId}
                    WHERE id = '{guid}'");
                if (rowsAffected != 1)
                {
                    this.plugin.log.Warning($"Start location UPDATE affected {rowsAffected} rows instead of 1");
                }
            }
        }

        public void EndLocationUpdate(string guid)
        {
            string timestamp = GetTimestamp();
            if (plugin.configuration.enableSaveData)
            {
                plugin.log.Info($"End location UPDATE {guid} updated = '{timestamp}', duty_end = '{timestamp}'");
                int rowsAffected = this.sqconn.Execute($@"UPDATE Roulettes SET updated = '{timestamp}', duty_end = '{timestamp}' WHERE id = '{guid}'");
                if (rowsAffected != 1)
                {
                    this.plugin.log.Warning($"End location UPDATE affected {rowsAffected} rows instead of 1");
                }
            }
        }

        public void DutyWipeUpdate(string guid)
        {
            string timestamp = GetTimestamp();
            if (plugin.configuration.enableSaveData)
            {
                plugin.log.Info($"Duty wipe UPDATE {guid} updated = '{timestamp}', wipes += 1");
                int rowsAffected = this.sqconn.Execute($@"UPDATE Roulettes SET updated = '{timestamp}', wipes = (IFNULL(wipes, 0) + 1) WHERE id = '{guid}'");
                if (rowsAffected != 1)
                {
                    this.plugin.log.Warning($"Duty wipe UPDATE affected {rowsAffected} rows instead of 1");
                }
            }
        }

        public void DutySuccessfulUpdate(string guid)
        {
            string timestamp = GetTimestamp();
            if (plugin.configuration.enableSaveData)
            {
                plugin.log.Info($"Duty success UPDATE {guid} updated = '{timestamp}', conclusion = 'clear'");
                int rowsAffected = this.sqconn.Execute($@"UPDATE Roulettes SET updated = '{timestamp}', conclusion = 'clear' WHERE id = '{guid}'");
                if (rowsAffected != 1)
                {
                    this.plugin.log.Warning($"Duty success UPDATE {rowsAffected} rows instead of 1");
                }
            }
        }

        private void InitializeDatabase()
        {
            const string create_tables = @"
                CREATE TABLE IF NOT EXISTS Roulettes (
                    id                  TEXT NOT NULL PRIMARY KEY,
                    created             NUMERIC NOT NULL,
                    updated             NUMERIC NOT NULL,
                    roulette_type       TEXT,
                    territory_id        INT,
                    content_id          INT,
                    queue_start         NUMERIC,
                    queue_end           NUMERIC,
                    duty_start          NUMERIC,
                    duty_end            NUMERIC,
                    conclusion          TEXT,
                    wipes               INT,
                    synclevel           INT
                );
                CREATE TABLE IF NOT EXISTS Players (
                    id                  TEXT NOT NULL PRIMARY KEY,
                    created             NUMERIC NOT NULL,
                    updated             NUMERIC NOT NULL,
                    name                TEXT,
                    homeworld           TEXT,
                    lodestone_id        INT,
                    collecter           INT
                );
                CREATE TABLE IF NOT EXISTS Gearsets (
                    id                  TEXT NOT NULL PRIMARY KEY,
                    created             NUMERIC NOT NULL,
                    updated             NUMERIC NOT NULL,
                    player              TEXT NOT NULL REFERENCES Players (id),
                    roulette            TEXT NOT NULL REFERENCES Roulettes (id),
                    item_level          INT,
                    job                 TEXT,
                    race                TEXT,
                    level               INT,
                    weapon              TEXT,
                    offhand             TEXT,
                    head                TEXT,
                    body                TEXT,
                    hands               TEXT,
                    legs                TEXT,
                    feet                TEXT,
                    ears                TEXT,
                    neck                TEXT,
                    wrists              TEXT,
                    ring1               TEXT,
                    ring2               TEXT,
                    materia_weapon      TEXT,
                    materia_offhand     TEXT,
                    materia_head        TEXT,
                    materia_body        TEXT,
                    materia_hands       TEXT,
                    materia_legs        TEXT,
                    materia_feet        TEXT,
                    materia_ears        TEXT,
                    materia_neck        TEXT,
                    materia_wrists      TEXT,
                    materia_ring1       TEXT,
                    materia_ring2       TEXT
                );";

            this.sqconn.Execute(create_tables);
        }
    }
}
