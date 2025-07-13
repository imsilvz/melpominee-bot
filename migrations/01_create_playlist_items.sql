CREATE TABLE IF NOT EXISTS "melpominee_audio"."playlist_tracks" (
	"id" BIGSERIAL PRIMARY KEY,
	"playlist_id" BIGINT NOT NULL REFERENCES "melpominee_audio"."playlists",
	"track_id" TEXT NOT NULL,
	UNIQUE ("playlist_id", "track_id")
);