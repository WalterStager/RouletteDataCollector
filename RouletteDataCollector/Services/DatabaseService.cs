using System;
using System.Reflection;

using Dapper;

using System.Data.SQLite;
using RouletteDataCollector.Structs;
using Dalamud.DrunkenToad.Extensions;
using System.Collections.Generic;

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

        // not for general use
        private static string getGuidFromList(List<string?> guids, int index)
        {
            if (index > guids.Count || index < 0)
            {
                return "NULL";
            }
            else
            {
                return $"'{guids[index]}'";
            }
        }

        public void GearsetGearUpdate(string guid, RDCGearset gear, List<string?> materiaSetGuids)
        {
            if (plugin.configuration.enableSaveData)
            {
                if (materiaSetGuids.Count > 12)
                {
                    throw new ArgumentException("materiaSetGuids should have <=12 values");
                }

                string timestamp = GetTimestamp();
                plugin.log.Info($"Gearsets gear UPDATE gear {gear}");
                int rowsAffected = this.sqconn.Execute($@"
                    UPDATE Gearsets SET
                        updated = '{timestamp}',
                        weapon = {gear.items[(int)RDCGearSlot.Weapon]},
                        offhand = {gear.items[(int)RDCGearSlot.Offhand]},
                        head = {gear.items[(int)RDCGearSlot.Head]},
                        body = {gear.items[(int)RDCGearSlot.Body]},
                        hands = {gear.items[(int)RDCGearSlot.Hands]},
                        legs = {gear.items[(int)RDCGearSlot.Legs]},
                        feet = {gear.items[(int)RDCGearSlot.Feet]},
                        ears = {gear.items[(int)RDCGearSlot.Ears]},
                        neck = {gear.items[(int)RDCGearSlot.Neck]},
                        wrists = {gear.items[(int)RDCGearSlot.Wrists]},
                        ring1 = {gear.items[(int)RDCGearSlot.Ring1]},
                        ring2 = {gear.items[(int)RDCGearSlot.Ring2]},
                        materia_weapon = {getGuidFromList(materiaSetGuids, 0)},
                        materia_offhand = {getGuidFromList(materiaSetGuids, 1)},
                        materia_head = {getGuidFromList(materiaSetGuids, 2)},
                        materia_body = {getGuidFromList(materiaSetGuids, 3)},
                        materia_hands = {getGuidFromList(materiaSetGuids, 4)},
                        materia_legs = {getGuidFromList(materiaSetGuids, 5)},
                        materia_feet = {getGuidFromList(materiaSetGuids, 6)},
                        materia_ears = {getGuidFromList(materiaSetGuids, 7)},
                        materia_neck = {getGuidFromList(materiaSetGuids, 8)},
                        materia_wrists = {getGuidFromList(materiaSetGuids, 9)},
                        materia_ring1 = {getGuidFromList(materiaSetGuids, 10)},
                        materia_ring2 = {getGuidFromList(materiaSetGuids, 11)},
                        soulstone = {gear.soulstone}
                    WHERE id = '{guid}'");
                if (rowsAffected != 1)
                {
                    this.plugin.log.Warning($"Gearsets gear UPDATE affected {rowsAffected} rows instead of 1");
                }
            }
        }

        public void MateriasetInsert(string guid, RDCMateriaset materiaSet)
        {
            if (plugin.configuration.enableSaveData)
            {
                string timestamp = GetTimestamp();
                var data = new {
                    Guid = guid,
                    Created = timestamp,
                    Updated = timestamp,
                    materia0_type = materiaSet.materiaTypes[0],
                    materia1_type = materiaSet.materiaTypes[1],
                    materia2_type = materiaSet.materiaTypes[2],
                    materia3_type = materiaSet.materiaTypes[3],
                    materia4_type = materiaSet.materiaTypes[4],
                    materia0_grade = materiaSet.materiaGrades[0],
                    materia1_grade = materiaSet.materiaGrades[1],
                    materia2_grade = materiaSet.materiaGrades[2],
                    materia3_grade = materiaSet.materiaGrades[3],
                    materia4_grade = materiaSet.materiaGrades[4]};
                plugin.log.Info($"Materiaset INSERT {data}");
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
                        @Guid,
                        @Created,
                        @Updated,
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
                    )", data);
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
                CREATE TABLE IF NOT EXISTS Materiasets (
                    id                  TEXT NOT NULL PRIMARY KEY,
                    created             NUMERIC NOT NULL,
                    updated             NUMERIC NOT NULL,
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
                    created             NUMERIC NOT NULL,
                    updated             NUMERIC NOT NULL,
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
}
