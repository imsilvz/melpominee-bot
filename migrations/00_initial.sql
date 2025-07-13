CREATE SCHEMA IF NOT EXISTS "melpominee_audio";
CREATE TABLE IF NOT EXISTS "melpominee_audio"."playlists" (
	"id" BIGSERIAL PRIMARY KEY,
	"owner" BIGINT NOT NULL,
	"playlist_name" TEXT NOT NULL,
	"created_at" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
	"updated_at" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
	UNIQUE ("owner", "playlist_name")
);