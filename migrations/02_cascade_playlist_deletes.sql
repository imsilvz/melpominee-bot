ALTER TABLE "melpominee_audio"."playlist_tracks" DROP CONSTRAINT playlist_tracks_playlist_id_fkey;
ALTER TABLE "melpominee_audio"."playlist_tracks"
    ADD constraint playlist_tracks_playlist_id_fkey 
    FOREIGN KEY (playlist_id) REFERENCES "melpominee_audio"."playlists"(id)
    ON DELETE CASCADE;