using System;
using System.Reflection;

using Dapper;

using System.Data.SQLite;
using RouletteDataCollector.Structs;
using Dalamud.DrunkenToad.Extensions;

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

        public void PlayerInsert(string guid, RDCPartyMember player)
        {            
            if (plugin.configuration.enableSaveData)
            {
                string timestamp = GetTimestamp();
                var data = new {Guid = guid, Created = timestamp, Updated = timestamp, Name = player.name, Homeworld = player.homeworldId, Collector = player.collector};
                plugin.log.Info($"Player INSERT {data}");
                int rowsAffected = this.sqconn.Execute("INSERT INTO Players (id, created, updated, name, homeworld, collector) VALUES (@Guid, @Created, @Updated, @Name, @Homeworld, @Collector)", data);
                if (rowsAffected != 1)
                {
                    this.plugin.log.Warning($"Player INSERT affected {rowsAffected} rows instead of 1");
                }
            }
        }

        public void GearsetInsert(string guid, string playerGuid, string rouletteGuid, uint job, uint level)
        {
            if (plugin.configuration.enableSaveData)
            {
                string timestamp = GetTimestamp();
                var data = new {Guid = guid, Created = timestamp, Updated = timestamp, Player = playerGuid, Roulette = rouletteGuid, Job = job, Level = level};
                plugin.log.Info($"Gearset INSERT {data}");
                int rowsAffected = this.sqconn.Execute("INSERT INTO Gearsets (id, created, updated, player, roulette, job, level) VALUES (@Guid, @Created, @Updated, @Player, @Roulette, @Job, @Level)", data);
                if (rowsAffected != 1)
                {
                    this.plugin.log.Warning($"Gearset INSERT affected {rowsAffected} rows instead of 1");
                }
            }   
        }

        private void InitializeDatabase()
        {
            string create_tables = $@"
                PRAGMA user_version = {Assembly.GetCallingAssembly().VersionNumber()};
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
                    homeworld           INT,
                    lodestone_id        INT,
                    collector           INT
                );
                CREATE TABLE IF NOT EXISTS Gearsets (
                    id                  TEXT NOT NULL PRIMARY KEY,
                    created             NUMERIC NOT NULL,
                    updated             NUMERIC NOT NULL,
                    player              TEXT NOT NULL REFERENCES Players (id),
                    roulette            TEXT NOT NULL REFERENCES Roulettes (id),
                    item_level          INT,
                    job                 INT,
                    race                INT,
                    level               INT,
                    weapon              INT,
                    offhand             INT,
                    head                INT,
                    body                INT,
                    hands               INT,
                    legs                INT,
                    feet                INT,
                    ears                INT,
                    neck                INT,
                    wrists              INT,
                    ring1               INT,
                    ring2               INT,
                    materia_weapon      INT,
                    materia_offhand     INT,
                    materia_head        INT,
                    materia_body        INT,
                    materia_hands       INT,
                    materia_legs        INT,
                    materia_feet        INT,
                    materia_ears        INT,
                    materia_neck        INT,
                    materia_wrists      INT,
                    materia_ring1       INT,
                    materia_ring2       INT
                );";

            this.sqconn.Execute(create_tables);
        }
    }
}
