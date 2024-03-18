using System;
using System.Reflection;

using Dapper;

using System.Data.SQLite;
using RouletteDataCollector.Structs;
using Dalamud.DrunkenToad.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Data;

namespace RouletteDataCollector.Services;

public sealed class DatabaseService
{
    private static Dictionary<RDCDatabaseTable, string> EnumToDBName = new Dictionary<RDCDatabaseTable, string>{
            {RDCDatabaseTable.Gearsets, "Gearsets"},
            {RDCDatabaseTable.Roulettes, "Roulettes"},
            {RDCDatabaseTable.Players, "Players"},
            {RDCDatabaseTable.Materiasets, "Materiasets"}
        };

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

    public static string GetTimestampString()
    {
        return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.ff");
    }

    public static DateTime GetTimestamp()
    {
        return DateTime.UtcNow;
    }

    public void RouletteInsert(string guid, string rouletteType)
    {
        if (plugin.configuration.enableSaveData)
        {
            DateTime timestamp = GetTimestamp();
            var data = new {Guid = guid, Created = timestamp, Updated = timestamp, QueueStart = timestamp, RouletteType=rouletteType};
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
        if (plugin.configuration.enableSaveData)
        {
            DateTime timestamp = GetTimestamp();
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
        if (plugin.configuration.enableSaveData)
        {
            DateTime timestamp = GetTimestamp();
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
        if (plugin.configuration.enableSaveData)
        {
            DateTime timestamp = GetTimestamp();
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
        if (plugin.configuration.enableSaveData)
        {
            DateTime timestamp = GetTimestamp();
            plugin.log.Info($"Duty success UPDATE {guid} updated = '{timestamp}', conclusion = 'clear'");
            int rowsAffected = this.sqconn.Execute($@"UPDATE Roulettes SET updated = '{timestamp}', conclusion = 'clear' WHERE id = '{guid}'");
            if (rowsAffected != 1)
            {
                this.plugin.log.Warning($"Duty success UPDATE {rowsAffected} rows instead of 1");
            }
        }
    }

    public void PlayerInsert(string guid, DBPlayer player)
    {            
        if (plugin.configuration.enableSaveData)
        {
            DateTime timestamp = GetTimestamp();
            player.id = guid;
            player.created = timestamp;
            player.updated = timestamp;
            plugin.log.Info($"Player INSERT {player}");
            int rowsAffected = this.sqconn.Execute("INSERT OR IGNORE INTO Players (id, created, updated, name, homeworld, collector) VALUES (@id, @created, @updated, @name, @homeworld, @collector)", player);
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
            DateTime timestamp = GetTimestamp();
            var data = new {Guid = guid, Created = timestamp, Updated = timestamp, Player = playerGuid, Roulette = rouletteGuid, Job = job, Level = level};
            plugin.log.Info($"Gearset INSERT {data}");
            int rowsAffected = this.sqconn.Execute(@"
                INSERT INTO Gearsets
                (
                    id,
                    created,
                    updated,
                    player,
                    roulette,
                    job,
                    level
                )
                VALUES
                (
                    @Guid,
                    @Created,
                    @Updated,
                    @Player,
                    @Roulette,
                    @Job,
                    @Level
                )", data);
            if (rowsAffected != 1)
            {
                this.plugin.log.Warning($"Gearset INSERT affected {rowsAffected} rows instead of 1");
            }
        }
    }

    public void GearsetGearUpdate(string guid, DBGearset gear)
    {
        if (plugin.configuration.enableSaveData)
        {
            gear.updated = GetTimestamp();
            plugin.log.Info($"Gearsets gear UPDATE {guid} {gear}");
            int rowsAffected = this.sqconn.Execute($@"
                UPDATE Gearsets SET
                    updated = @updated,
                    race = @race,
                    weapon = @weapon,
                    offhand = @offhand,
                    head = @head,
                    body = @body,
                    hands = @hands,
                    legs = @legs,
                    feet = @feet,
                    ears = @ears,
                    neck = @neck,
                    wrists = @wrists,
                    ring1 = @ring1,
                    ring2 = @ring2,
                    materia_weapon = @materia_weapon,
                    materia_offhand = @materia_offhand,
                    materia_head = @materia_head,
                    materia_body = @materia_body,
                    materia_hands = @materia_hands,
                    materia_legs = @materia_legs,
                    materia_feet = @materia_feet,
                    materia_ears = @materia_ears,
                    materia_neck = @materia_neck,
                    materia_wrists = @materia_wrists,
                    materia_ring1 = @materia_ring1,
                    materia_ring2 = @materia_ring2,
                    soulstone = @soulstone
                WHERE id = '{guid}'", gear);
            if (rowsAffected != 1)
            {
                this.plugin.log.Warning($"Gearsets gear UPDATE affected {rowsAffected} rows instead of 1");
            }
        }
    }

    public IEnumerable<T> QueryRecentlyUpdated<T>(RDCDatabaseTable table, uint limit, uint offset)
    {
        string query = $@"
            SELECT * FROM {EnumToDBName[table]}
                ORDER BY updated DESC
                LIMIT {limit}
                OFFSET {offset}";

        return this.sqconn.Query<T>(query);
    }

    public void MateriasetInsert(string guid, DBMateriaset materiaSet)
    {
        if (plugin.configuration.enableSaveData)
        {
            DateTime timestamp = GetTimestamp();
            materiaSet.id = guid;
            materiaSet.created = timestamp;
            materiaSet.updated = timestamp;
            plugin.log.Info($"Materiaset INSERT {materiaSet}");
            int rowsAffected = this.sqconn.Execute(@"
                INSERT INTO Materiasets (
                    id,
                    created,
                    updated,
                    materia0_type,
                    materia1_type,
                    materia2_type,
                    materia3_type,
                    materia4_type,
                    materia0_grade,
                    materia1_grade,
                    materia2_grade,
                    materia3_grade,
                    materia4_grade
                ) VALUES (
                    @id,
                    @created,
                    @updated,
                    @materia0_type,
                    @materia1_type,
                    @materia2_type,
                    @materia3_type,
                    @materia4_type,
                    @materia0_grade,
                    @materia1_grade,
                    @materia2_grade,
                    @materia3_grade,
                    @materia4_grade
                )", materiaSet);
            if (rowsAffected != 1)
            {
                this.plugin.log.Warning($"Materiaset INSERT affected {rowsAffected} rows instead of 1");
            }
        }
    }

    private void InitializeDatabase()
    {
        string create_tables = $@"
            PRAGMA user_version = {Assembly.GetCallingAssembly().VersionNumber()};
            CREATE TABLE IF NOT EXISTS Roulettes (
                id                  TEXT NOT NULL PRIMARY KEY,
                created             TEXT NOT NULL,
                updated             TEXT NOT NULL,
                roulette_type       TEXT,
                territory_id        INT,
                content_id          INT,
                queue_start         TEXT,
                queue_end           TEXT,
                duty_start          TEXT,
                duty_end            TEXT,
                conclusion          TEXT,
                wipes               INT,
                synclevel           INT
            );
            CREATE TABLE IF NOT EXISTS Players (
                id                  TEXT NOT NULL PRIMARY KEY,
                created             TEXT NOT NULL,
                updated             TEXT NOT NULL,
                name                TEXT,
                homeworld           INT,
                lodestone_id        INT,
                collector           INT
            );
            CREATE TABLE IF NOT EXISTS Materiasets (
                id                  TEXT NOT NULL PRIMARY KEY,
                created             TEXT NOT NULL,
                updated             TEXT NOT NULL,
                materia0_type       INT,
                materia1_type       INT,
                materia2_type       INT,
                materia3_type       INT,
                materia4_type       INT,
                materia0_grade      INT,
                materia1_grade      INT,
                materia2_grade      INT,
                materia3_grade      INT,
                materia4_grade      INT
            );
            CREATE TABLE IF NOT EXISTS Gearsets (
                id                  TEXT NOT NULL PRIMARY KEY,
                created             TEXT NOT NULL,
                updated             TEXT NOT NULL,
                player              TEXT NOT NULL REFERENCES Players (id),
                roulette            TEXT NOT NULL REFERENCES Roulettes (id),
                item_level          INT,
                job                 INT,
                race                INT,
                level               INT,
                left_early          INT,
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
                soulstone           INT,
                materia_weapon      TEXT REFERENCES Materiasets (id),
                materia_offhand     TEXT REFERENCES Materiasets (id),
                materia_head        TEXT REFERENCES Materiasets (id),
                materia_body        TEXT REFERENCES Materiasets (id),
                materia_hands       TEXT REFERENCES Materiasets (id),
                materia_legs        TEXT REFERENCES Materiasets (id),
                materia_feet        TEXT REFERENCES Materiasets (id),
                materia_ears        TEXT REFERENCES Materiasets (id),
                materia_neck        TEXT REFERENCES Materiasets (id),
                materia_wrists      TEXT REFERENCES Materiasets (id),
                materia_ring1       TEXT REFERENCES Materiasets (id),
                materia_ring2       TEXT REFERENCES Materiasets (id)                    
            );";

        this.sqconn.Execute(create_tables);
    }
}
