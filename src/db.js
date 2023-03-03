import sqlite3 from 'sqlite3'
import { open } from 'sqlite'

const db = await open({
    filename: 'database.db',
    driver: sqlite3.Database
});

await db.exec(`
    CREATE TABLE IF NOT EXISTS settings (
        guild TEXT,
        setting TEXT,
        value TEXT NOT NULL,
        
        PRIMARY KEY(guild, setting)
    )
`);

await db.exec(`
    CREATE TABLE IF NOT EXISTS volumes (
        guild TEXT NOT NULL,
        playlist TEXT NOT NULL,
        song TEXT NOT NULL,
        volume TEXT,

        PRIMARY KEY(guild, playlist, song),
        UNIQUE(guild, playlist, song)
    )
`);

export { db };