BEGIN TRANSACTION;

DROP FUNCTION note_replies(start_id character varying, max_depth integer, max_breadth integer);

DROP TABLE __chart__active_users;
DROP TABLE __chart__ap_request;
DROP TABLE __chart__drive;
DROP TABLE __chart__federation;
DROP TABLE __chart__hashtag;
DROP TABLE __chart__instance;
DROP TABLE __chart__network;
DROP TABLE __chart__notes;
DROP TABLE __chart__per_user_drive;
DROP TABLE __chart__per_user_following;
DROP TABLE __chart__per_user_notes;
DROP TABLE __chart__per_user_reaction;
DROP TABLE __chart__test;
DROP TABLE __chart__test_grouped;
DROP TABLE __chart__test_unique;
DROP TABLE __chart__users;
DROP TABLE __chart_day__active_users;
DROP TABLE __chart_day__ap_request;
DROP TABLE __chart_day__drive;
DROP TABLE __chart_day__federation;
DROP TABLE __chart_day__hashtag;
DROP TABLE __chart_day__instance;
DROP TABLE __chart_day__network;
DROP TABLE __chart_day__notes;
DROP TABLE __chart_day__per_user_drive;
DROP TABLE __chart_day__per_user_following;
DROP TABLE __chart_day__per_user_notes;
DROP TABLE __chart_day__per_user_reaction;
DROP TABLE __chart_day__users;
DROP TABLE migrations;
DROP TABLE user_ip;

ALTER TABLE user_publickey DROP CONSTRAINT "PK_10c146e4b39b443ede016f6736d";
ALTER TABLE "user" DROP CONSTRAINT "REL_58f5c71eaab331645112cf8cfa";
ALTER TABLE "user" DROP CONSTRAINT "REL_afc64b53f8db3707ceb34eb28e";
ALTER TABLE "user" DROP CONSTRAINT "UQ_a854e557b1b14814750c7c7b0c9";
ALTER TABLE user_profile DROP CONSTRAINT "UQ_6dc44f1ceb65b1e72bacef2ca27";

CREATE INDEX "IX_abuse_user_report_assigneeId" ON abuse_user_report USING btree ("assigneeId");
CREATE INDEX "IX_access_token_appId" ON access_token USING btree ("appId");
CREATE INDEX "IX_antenna_userGroupJoiningId" ON antenna USING btree ("userGroupJoiningId");
CREATE INDEX "IX_antenna_userListId" ON antenna USING btree ("userListId");
CREATE INDEX "IX_auth_session_appId" ON auth_session USING btree ("appId");
CREATE INDEX "IX_auth_session_userId" ON auth_session USING btree ("userId");
CREATE INDEX "IX_channel_bannerId" ON channel USING btree ("bannerId");
CREATE INDEX "IX_channel_note_pining_noteId" ON channel_note_pining USING btree ("noteId");
CREATE INDEX "IX_gallery_like_postId" ON gallery_like USING btree ("postId");
CREATE INDEX "IX_messaging_message_fileId" ON messaging_message USING btree ("fileId");
CREATE INDEX "IX_note_favorite_noteId" ON note_favorite USING btree ("noteId");
CREATE INDEX "IX_notification_followRequestId" ON notification USING btree ("followRequestId");
CREATE INDEX "IX_notification_noteId" ON notification USING btree ("noteId");
CREATE INDEX "IX_notification_userGroupInvitationId" ON notification USING btree ("userGroupInvitationId");
CREATE INDEX "IX_oauth_token_appId" ON oauth_token USING btree ("appId");
CREATE INDEX "IX_oauth_token_userId" ON oauth_token USING btree ("userId");
CREATE INDEX "IX_page_eyeCatchingImageId" ON page USING btree ("eyeCatchingImageId");
CREATE INDEX "IX_page_like_pageId" ON page_like USING btree ("pageId");
CREATE INDEX "IX_promo_read_noteId" ON promo_read USING btree ("noteId");
CREATE INDEX "IX_session_userId" ON "session" USING btree ("userId");
CREATE INDEX "IX_user_note_pining_noteId" ON user_note_pining USING btree ("noteId");

CREATE UNIQUE INDEX "REL_58f5c71eaab331645112cf8cfa" ON "user" USING btree ("avatarId");
CREATE UNIQUE INDEX "REL_afc64b53f8db3707ceb34eb28e" ON "user" USING btree ("bannerId");
CREATE UNIQUE INDEX "UQ_a854e557b1b14814750c7c7b0c9" ON "user" USING btree (token);
CREATE UNIQUE INDEX "UQ_6dc44f1ceb65b1e72bacef2ca27" ON user_profile USING btree ("pinnedPageId");

CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ('20240107155500_Initial', '8.0.0');

COMMIT;
