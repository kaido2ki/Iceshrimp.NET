using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenameIndicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IDX_a854e557b1b14814750c7c7b0c",
                table: "user");

            migrationBuilder.RenameIndex(
                name: "IDX_f272c8c8805969e6a6449c77b3",
                table: "webhook",
                newName: "IX_webhook_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_8063a0586ed1dfbe86e982d961",
                table: "webhook",
                newName: "IX_webhook_on");

            migrationBuilder.RenameIndex(
                name: "IDX_5a056076f76b2efe08216ba655",
                table: "webhook",
                newName: "IX_webhook_active");

            migrationBuilder.RenameIndex(
                name: "IDX_ff9ca3b5f3ee3d0681367a9b44",
                table: "user_security_key",
                newName: "IX_user_security_key_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_0d7718e562dcedd0aa5cf2c9f7",
                table: "user_security_key",
                newName: "IX_user_security_key_publicKey");

            migrationBuilder.RenameIndex(
                name: "IDX_171e64971c780ebd23fae140bb",
                table: "user_publickey",
                newName: "IX_user_publickey_keyId");

            migrationBuilder.RenameIndex(
                name: "UQ_6dc44f1ceb65b1e72bacef2ca27",
                table: "user_profile",
                newName: "IX_user_profile_pinnedPageId");

            migrationBuilder.RenameIndex(
                name: "IDX_dce530b98e454793dac5ec2f5a",
                table: "user_profile",
                newName: "IX_user_profile_userHost");

            migrationBuilder.RenameIndex(
                name: "IDX_3befe6f999c86aff06eb0257b4",
                table: "user_profile",
                newName: "IX_user_profile_enableWordMute");

            migrationBuilder.RenameIndex(
                name: "IDX_4e5c4c99175638ec0761714ab0",
                table: "user_pending",
                newName: "IX_user_pending_code");

            migrationBuilder.RenameIndex(
                name: "IDX_bfbc6f79ba4007b4ce5097f08d",
                table: "user_note_pin",
                newName: "IX_user_note_pin_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_410cd649884b501c02d6e72738",
                table: "user_note_pin",
                newName: "IX_user_note_pin_userId_noteId");

            migrationBuilder.RenameIndex(
                name: "IDX_d844bfc6f3f523a05189076efa",
                table: "user_list_member",
                newName: "IX_user_list_member_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_90f7da835e4c10aca6853621e1",
                table: "user_list_member",
                newName: "IX_user_list_member_userId_userListId");

            migrationBuilder.RenameIndex(
                name: "IDX_605472305f26818cc93d1baaa7",
                table: "user_list_member",
                newName: "IX_user_list_member_userListId");

            migrationBuilder.RenameIndex(
                name: "IDX_b7fcefbdd1c18dce86687531f9",
                table: "user_list",
                newName: "IX_user_list_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_f3a1b4bd0c7cabba958a0c0b23",
                table: "user_group_member",
                newName: "IX_user_group_member_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_d9ecaed8c6dc43f3592c229282",
                table: "user_group_member",
                newName: "IX_user_group_member_userId_userGroupId");

            migrationBuilder.RenameIndex(
                name: "IDX_67dc758bc0566985d1b3d39986",
                table: "user_group_member",
                newName: "IX_user_group_member_userGroupId");

            migrationBuilder.RenameIndex(
                name: "IDX_e10924607d058004304611a436",
                table: "user_group_invite",
                newName: "IX_user_group_invite_userGroupId");

            migrationBuilder.RenameIndex(
                name: "IDX_78787741f9010886796f2320a4",
                table: "user_group_invite",
                newName: "IX_user_group_invite_userId_userGroupId");

            migrationBuilder.RenameIndex(
                name: "IDX_1039988afa3bf991185b277fe0",
                table: "user_group_invite",
                newName: "IX_user_group_invite_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_e9793f65f504e5a31fbaedbf2f",
                table: "user_group_invitation",
                newName: "IX_user_group_invitation_userId_userGroupId");

            migrationBuilder.RenameIndex(
                name: "IDX_bfbc6305547539369fe73eb144",
                table: "user_group_invitation",
                newName: "IX_user_group_invitation_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_5cc8c468090e129857e9fecce5",
                table: "user_group_invitation",
                newName: "IX_user_group_invitation_userGroupId");

            migrationBuilder.RenameIndex(
                name: "IDX_3d6b372788ab01be58853003c9",
                table: "user_group",
                newName: "IX_user_group_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_20e30aa35180e317e133d75316",
                table: "user_group",
                newName: "IX_user_group_createdAt");

            migrationBuilder.RenameIndex(
                name: "UQ_a854e557b1b14814750c7c7b0c9",
                table: "user",
                newName: "IX_user_token");

            migrationBuilder.RenameIndex(
                name: "REL_afc64b53f8db3707ceb34eb28e",
                table: "user",
                newName: "IX_user_bannerId");

            migrationBuilder.RenameIndex(
                name: "REL_58f5c71eaab331645112cf8cfa",
                table: "user",
                newName: "IX_user_avatarId");

            migrationBuilder.RenameIndex(
                name: "IDX_fa99d777623947a5b05f394cae",
                table: "user",
                newName: "IX_user_tags");

            migrationBuilder.RenameIndex(
                name: "IDX_e11e649824a45d8ed01d597fd9",
                table: "user",
                newName: "IX_user_createdAt");

            migrationBuilder.RenameIndex(
                name: "IDX_d5a1b83c7cab66f167e6888188",
                table: "user",
                newName: "IX_user_isExplorable");

            migrationBuilder.RenameIndex(
                name: "IDX_c8cc87bd0f2f4487d17c651fbf",
                table: "user",
                newName: "IX_user_lastActiveDate");

            migrationBuilder.RenameIndex(
                name: "IDX_be623adaa4c566baf5d29ce0c8",
                table: "user",
                newName: "IX_user_uri");

            migrationBuilder.RenameIndex(
                name: "IDX_a27b942a0d6dcff90e3ee9b5e8",
                table: "user",
                newName: "IX_user_usernameLower");

            migrationBuilder.RenameIndex(
                name: "IDX_80ca6e6ef65fb9ef34ea8c90f4",
                table: "user",
                newName: "IX_user_updatedAt");

            migrationBuilder.RenameIndex(
                name: "IDX_5deb01ae162d1d70b80d064c27",
                table: "user",
                newName: "IX_user_usernameLower_host");

            migrationBuilder.RenameIndex(
                name: "IDX_3252a5df8d5bbd16b281f7799e",
                table: "user",
                newName: "IX_user_host");

            migrationBuilder.RenameIndex(
                name: "IDX_97754ca6f2baff9b4abb7f853d",
                table: "sw_subscription",
                newName: "IX_sw_subscription_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_2c308dbdc50d94dc625670055f",
                table: "signin",
                newName: "IX_signin_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_232f8e85d7633bd6ddfad42169",
                table: "session",
                newName: "IX_session_token");

            migrationBuilder.RenameIndex(
                name: "IDX_d1259a2c2b7bb413ff449e8711",
                table: "renote_muting",
                newName: "IX_renote_muting_createdAt");

            migrationBuilder.RenameIndex(
                name: "IDX_7eac97594bcac5ffcf2068089b",
                table: "renote_muting",
                newName: "IX_renote_muting_muteeId");

            migrationBuilder.RenameIndex(
                name: "IDX_7aa72a5fe76019bfe8e5e0e8b7",
                table: "renote_muting",
                newName: "IX_renote_muting_muterId");

            migrationBuilder.RenameIndex(
                name: "IDX_0d801c609cec4e9eb4b6b4490c",
                table: "renote_muting",
                newName: "IX_renote_muting_muterId_muteeId");

            migrationBuilder.RenameIndex(
                name: "IDX_0d9a1738f2cf7f3b1c3334dfab",
                table: "relay",
                newName: "IX_relay_inbox");

            migrationBuilder.RenameIndex(
                name: "IDX_fb9d21ba0abb83223263df6bcb",
                table: "registry_item",
                newName: "IX_registry_item_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_22baca135bb8a3ea1a83d13df3",
                table: "registry_item",
                newName: "IX_registry_item_scope");

            migrationBuilder.RenameIndex(
                name: "IDX_0a72bdfcdb97c0eca11fe7ecad",
                table: "registry_item",
                newName: "IX_registry_item_domain");

            migrationBuilder.RenameIndex(
                name: "IDX_0ff69e8dfa9fe31bb4a4660f59",
                table: "registration_ticket",
                newName: "IX_registration_ticket_code");

            migrationBuilder.RenameIndex(
                name: "IDX_9657d55550c3d37bfafaf7d4b0",
                table: "promo_read",
                newName: "IX_promo_read_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_2882b8a1a07c7d281a98b6db16",
                table: "promo_read",
                newName: "IX_promo_read_userId_noteId");

            migrationBuilder.RenameIndex(
                name: "IDX_83f0862e9bae44af52ced7099e",
                table: "promo_note",
                newName: "IX_promo_note_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_aecfbd5ef60374918e63ee95fa",
                table: "poll_vote",
                newName: "IX_poll_vote_noteId");

            migrationBuilder.RenameIndex(
                name: "IDX_66d2bd2ee31d14bcc23069a89f",
                table: "poll_vote",
                newName: "IX_poll_vote_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_50bd7164c5b78f1f4a42c4d21f",
                table: "poll_vote",
                newName: "IX_poll_vote_userId_noteId_choice");

            migrationBuilder.RenameIndex(
                name: "IDX_0fb627e1c2f753262a74f0562d",
                table: "poll_vote",
                newName: "IX_poll_vote_createdAt");

            migrationBuilder.RenameIndex(
                name: "IDX_7fa20a12319c7f6dc3aed98c0a",
                table: "poll",
                newName: "IX_poll_userHost");

            migrationBuilder.RenameIndex(
                name: "IDX_0610ebcfcfb4a18441a9bcdab2",
                table: "poll",
                newName: "IX_poll_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_4bb7fd4a34492ae0e6cc8d30ac",
                table: "password_reset_request",
                newName: "IX_password_reset_request_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_0b575fa9a4cfe638a925949285",
                table: "password_reset_request",
                newName: "IX_password_reset_request_token");

            migrationBuilder.RenameIndex(
                name: "IDX_4ce6fb9c70529b4c8ac46c9bfa",
                table: "page_like",
                newName: "IX_page_like_userId_pageId");

            migrationBuilder.RenameIndex(
                name: "IDX_0e61efab7f88dbb79c9166dbb4",
                table: "page_like",
                newName: "IX_page_like_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_fbb4297c927a9b85e9cefa2eb1",
                table: "page",
                newName: "IX_page_createdAt");

            migrationBuilder.RenameIndex(
                name: "IDX_b82c19c08afb292de4600d99e4",
                table: "page",
                newName: "IX_page_name");

            migrationBuilder.RenameIndex(
                name: "IDX_af639b066dfbca78b01a920f8a",
                table: "page",
                newName: "IX_page_updatedAt");

            migrationBuilder.RenameIndex(
                name: "IDX_ae1d917992dd0c9d9bbdad06c4",
                table: "page",
                newName: "IX_page_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_90148bbc2bf0854428786bfc15",
                table: "page",
                newName: "IX_page_visibleUserIds");

            migrationBuilder.RenameIndex(
                name: "IDX_2133ef8317e4bdb839c0dcbf13",
                table: "page",
                newName: "IX_page_userId_name");

            migrationBuilder.RenameIndex(
                name: "IDX_dc5fe174a8b59025055f0ec136",
                table: "oauth_token",
                newName: "IX_oauth_token_code");

            migrationBuilder.RenameIndex(
                name: "IDX_2cbeb4b389444bcf4379ef4273",
                table: "oauth_token",
                newName: "IX_oauth_token_token");

            migrationBuilder.RenameIndex(
                name: "IDX_65b61f406c811241e1315a2f82",
                table: "oauth_app",
                newName: "IX_oauth_app_clientId");

            migrationBuilder.RenameIndex(
                name: "IDX_e22bf6bda77b6adc1fd9e75c8c",
                table: "notification",
                newName: "IX_notification_appAccessTokenId");

            migrationBuilder.RenameIndex(
                name: "IDX_b11a5e627c41d4dc3170f1d370",
                table: "notification",
                newName: "IX_notification_createdAt");

            migrationBuilder.RenameIndex(
                name: "IDX_3c601b70a1066d2c8b517094cb",
                table: "notification",
                newName: "IX_notification_notifieeId");

            migrationBuilder.RenameIndex(
                name: "IDX_3b4e96eec8d36a8bbb9d02aa71",
                table: "notification",
                newName: "IX_notification_notifierId");

            migrationBuilder.RenameIndex(
                name: "IDX_33f33cc8ef29d805a97ff4628b",
                table: "notification",
                newName: "IX_notification_type");

            migrationBuilder.RenameIndex(
                name: "IDX_080ab397c379af09b9d2169e5b",
                table: "notification",
                newName: "IX_notification_isRead");

            migrationBuilder.RenameIndex(
                name: "IDX_b0134ec406e8d09a540f818288",
                table: "note_watching",
                newName: "IX_note_watching_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_a42c93c69989ce1d09959df4cf",
                table: "note_watching",
                newName: "IX_note_watching_userId_noteId");

            migrationBuilder.RenameIndex(
                name: "IDX_44499765eec6b5489d72c4253b",
                table: "note_watching",
                newName: "IX_note_watching_noteUserId");

            migrationBuilder.RenameIndex(
                name: "IDX_318cdf42a9cfc11f479bd802bb",
                table: "note_watching",
                newName: "IX_note_watching_createdAt");

            migrationBuilder.RenameIndex(
                name: "IDX_03e7028ab8388a3f5e3ce2a861",
                table: "note_watching",
                newName: "IX_note_watching_noteId");

            migrationBuilder.RenameIndex(
                name: "IDX_e637cba4dc4410218c4251260e",
                table: "note_unread",
                newName: "IX_note_unread_noteId");

            migrationBuilder.RenameIndex(
                name: "IDX_d908433a4953cc13216cd9c274",
                table: "note_unread",
                newName: "IX_note_unread_userId_noteId");

            migrationBuilder.RenameIndex(
                name: "IDX_89a29c9237b8c3b6b3cbb4cb30",
                table: "note_unread",
                newName: "IX_note_unread_isSpecified");

            migrationBuilder.RenameIndex(
                name: "IDX_6a57f051d82c6d4036c141e107",
                table: "note_unread",
                newName: "IX_note_unread_noteChannelId");

            migrationBuilder.RenameIndex(
                name: "IDX_56b0166d34ddae49d8ef7610bb",
                table: "note_unread",
                newName: "IX_note_unread_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_29e8c1d579af54d4232939f994",
                table: "note_unread",
                newName: "IX_note_unread_noteUserId");

            migrationBuilder.RenameIndex(
                name: "IDX_25b1dd384bec391b07b74b861c",
                table: "note_unread",
                newName: "IX_note_unread_isMentioned");

            migrationBuilder.RenameIndex(
                name: "IDX_c426394644267453e76f036926",
                table: "note_thread_muting",
                newName: "IX_note_thread_muting_threadId");

            migrationBuilder.RenameIndex(
                name: "IDX_ae7aab18a2641d3e5f25e0c4ea",
                table: "note_thread_muting",
                newName: "IX_note_thread_muting_userId_threadId");

            migrationBuilder.RenameIndex(
                name: "IDX_29c11c7deb06615076f8c95b80",
                table: "note_thread_muting",
                newName: "IX_note_thread_muting_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_ad0c221b25672daf2df320a817",
                table: "note_reaction",
                newName: "IX_note_reaction_userId_noteId");

            migrationBuilder.RenameIndex(
                name: "IDX_45145e4953780f3cd5656f0ea6",
                table: "note_reaction",
                newName: "IX_note_reaction_noteId");

            migrationBuilder.RenameIndex(
                name: "IDX_13761f64257f40c5636d0ff95e",
                table: "note_reaction",
                newName: "IX_note_reaction_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_01f4581f114e0ebd2bbb876f0b",
                table: "note_reaction",
                newName: "IX_note_reaction_createdAt");

            migrationBuilder.RenameIndex(
                name: "IDX_47f4b1892f5d6ba8efb3057d81",
                table: "note_favorite",
                newName: "IX_note_favorite_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_0f4fb9ad355f3effff221ef245",
                table: "note_favorite",
                newName: "IX_note_favorite_userId_noteId");

            migrationBuilder.RenameIndex(
                name: "IDX_702ad5ae993a672e4fbffbcd38",
                table: "note_edit",
                newName: "IX_note_edit_noteId");

            migrationBuilder.RenameIndex(
                name: "IDX_note_userId_id",
                table: "note",
                newName: "IX_note_userId_id");

            migrationBuilder.RenameIndex(
                name: "IDX_note_url",
                table: "note",
                newName: "IX_note_url");

            migrationBuilder.RenameIndex(
                name: "IDX_note_id_userHost",
                table: "note",
                newName: "IX_note_id_userHost");

            migrationBuilder.RenameIndex(
                name: "IDX_note_createdAt_userId",
                table: "note",
                newName: "IX_note_createdAt_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_f22169eb10657bded6d875ac8f",
                table: "note",
                newName: "IX_note_channelId");

            migrationBuilder.RenameIndex(
                name: "IDX_e7c0567f5261063592f022e9b5",
                table: "note",
                newName: "IX_note_createdAt");

            migrationBuilder.RenameIndex(
                name: "IDX_d4ebdef929896d6dc4a3c5bb48",
                table: "note",
                newName: "IX_note_threadId");

            migrationBuilder.RenameIndex(
                name: "IDX_88937d94d7443d9a99a76fa5c0",
                table: "note",
                newName: "IX_note_tags");

            migrationBuilder.RenameIndex(
                name: "IDX_796a8c03959361f97dc2be1d5c",
                table: "note",
                newName: "IX_note_visibleUserIds");

            migrationBuilder.RenameIndex(
                name: "IDX_7125a826ab192eb27e11d358a5",
                table: "note",
                newName: "IX_note_userHost");

            migrationBuilder.RenameIndex(
                name: "IDX_5b87d9d19127bd5d92026017a7",
                table: "note",
                newName: "IX_note_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_54ebcb6d27222913b908d56fd8",
                table: "note",
                newName: "IX_note_mentions");

            migrationBuilder.RenameIndex(
                name: "IDX_52ccc804d7c69037d558bac4c9",
                table: "note",
                newName: "IX_note_renoteId");

            migrationBuilder.RenameIndex(
                name: "IDX_51c063b6a133a9cb87145450f5",
                table: "note",
                newName: "IX_note_fileIds");

            migrationBuilder.RenameIndex(
                name: "IDX_25dfc71b0369b003a4cd434d0b",
                table: "note",
                newName: "IX_note_attachedFileTypes");

            migrationBuilder.RenameIndex(
                name: "IDX_17cb3553c700a4985dff5a30ff",
                table: "note",
                newName: "IX_note_replyId");

            migrationBuilder.RenameIndex(
                name: "IDX_153536c67d05e9adb24e99fc2b",
                table: "note",
                newName: "IX_note_uri");

            migrationBuilder.RenameIndex(
                name: "IDX_f86d57fbca33c7a4e6897490cc",
                table: "muting",
                newName: "IX_muting_createdAt");

            migrationBuilder.RenameIndex(
                name: "IDX_ec96b4fed9dae517e0dbbe0675",
                table: "muting",
                newName: "IX_muting_muteeId");

            migrationBuilder.RenameIndex(
                name: "IDX_c1fd1c3dfb0627aa36c253fd14",
                table: "muting",
                newName: "IX_muting_expiresAt");

            migrationBuilder.RenameIndex(
                name: "IDX_93060675b4a79a577f31d260c6",
                table: "muting",
                newName: "IX_muting_muterId");

            migrationBuilder.RenameIndex(
                name: "IDX_1eb9d9824a630321a29fd3b290",
                table: "muting",
                newName: "IX_muting_muterId_muteeId");

            migrationBuilder.RenameIndex(
                name: "IDX_a08ad074601d204e0f69da9a95",
                table: "moderation_log",
                newName: "IX_moderation_log_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_e21cd3646e52ef9c94aaf17c2e",
                table: "messaging_message",
                newName: "IX_messaging_message_createdAt");

            migrationBuilder.RenameIndex(
                name: "IDX_cac14a4e3944454a5ce7daa514",
                table: "messaging_message",
                newName: "IX_messaging_message_recipientId");

            migrationBuilder.RenameIndex(
                name: "IDX_5377c307783fce2b6d352e1203",
                table: "messaging_message",
                newName: "IX_messaging_message_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_2c4be03b446884f9e9c502135b",
                table: "messaging_message",
                newName: "IX_messaging_message_groupId");

            migrationBuilder.RenameIndex(
                name: "IDX_8d5afc98982185799b160e10eb",
                table: "instance",
                newName: "IX_instance_host");

            migrationBuilder.RenameIndex(
                name: "IDX_34500da2e38ac393f7bb6b299c",
                table: "instance",
                newName: "IX_instance_isSuspended");

            migrationBuilder.RenameIndex(
                name: "IDX_2cd3b2a6b4cf0b910b260afe08",
                table: "instance",
                newName: "IX_instance_caughtAt");

            migrationBuilder.RenameIndex(
                name: "IDX_d57f9030cd3af7f63ffb1c267c",
                table: "hashtag",
                newName: "IX_hashtag_attachedUsersCount");

            migrationBuilder.RenameIndex(
                name: "IDX_4c02d38a976c3ae132228c6fce",
                table: "hashtag",
                newName: "IX_hashtag_mentionedRemoteUsersCount");

            migrationBuilder.RenameIndex(
                name: "IDX_347fec870eafea7b26c8a73bac",
                table: "hashtag",
                newName: "IX_hashtag_name");

            migrationBuilder.RenameIndex(
                name: "IDX_2710a55f826ee236ea1a62698f",
                table: "hashtag",
                newName: "IX_hashtag_mentionedUsersCount");

            migrationBuilder.RenameIndex(
                name: "IDX_0e206cec573f1edff4a3062923",
                table: "hashtag",
                newName: "IX_hashtag_mentionedLocalUsersCount");

            migrationBuilder.RenameIndex(
                name: "IDX_0c44bf4f680964145f2a68a341",
                table: "hashtag",
                newName: "IX_hashtag_attachedLocalUsersCount");

            migrationBuilder.RenameIndex(
                name: "IDX_0b03cbcd7e6a7ce068efa8ecc2",
                table: "hashtag",
                newName: "IX_hashtag_attachedRemoteUsersCount");

            migrationBuilder.RenameIndex(
                name: "IDX_f631d37835adb04792e361807c",
                table: "gallery_post",
                newName: "IX_gallery_post_updatedAt");

            migrationBuilder.RenameIndex(
                name: "IDX_f2d744d9a14d0dfb8b96cb7fc5",
                table: "gallery_post",
                newName: "IX_gallery_post_isSensitive");

            migrationBuilder.RenameIndex(
                name: "IDX_985b836dddd8615e432d7043dd",
                table: "gallery_post",
                newName: "IX_gallery_post_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_8f1a239bd077c8864a20c62c2c",
                table: "gallery_post",
                newName: "IX_gallery_post_createdAt");

            migrationBuilder.RenameIndex(
                name: "IDX_3ca50563facd913c425e7a89ee",
                table: "gallery_post",
                newName: "IX_gallery_post_fileIds");

            migrationBuilder.RenameIndex(
                name: "IDX_1a165c68a49d08f11caffbd206",
                table: "gallery_post",
                newName: "IX_gallery_post_likedCount");

            migrationBuilder.RenameIndex(
                name: "IDX_05cca34b985d1b8edc1d1e28df",
                table: "gallery_post",
                newName: "IX_gallery_post_tags");

            migrationBuilder.RenameIndex(
                name: "IDX_df1b5f4099e99fb0bc5eae53b6",
                table: "gallery_like",
                newName: "IX_gallery_like_userId_postId");

            migrationBuilder.RenameIndex(
                name: "IDX_8fd5215095473061855ceb948c",
                table: "gallery_like",
                newName: "IX_gallery_like_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_fcdafee716dfe9c3b5fde90f30",
                table: "following",
                newName: "IX_following_followeeHost");

            migrationBuilder.RenameIndex(
                name: "IDX_6516c5a6f3c015b4eed39978be",
                table: "following",
                newName: "IX_following_followerId");

            migrationBuilder.RenameIndex(
                name: "IDX_582f8fab771a9040a12961f3e7",
                table: "following",
                newName: "IX_following_createdAt");

            migrationBuilder.RenameIndex(
                name: "IDX_4ccd2239268ebbd1b35e318754",
                table: "following",
                newName: "IX_following_followerHost");

            migrationBuilder.RenameIndex(
                name: "IDX_307be5f1d1252e0388662acb96",
                table: "following",
                newName: "IX_following_followerId_followeeId");

            migrationBuilder.RenameIndex(
                name: "IDX_24e0042143a18157b234df186c",
                table: "following",
                newName: "IX_following_followeeId");

            migrationBuilder.RenameIndex(
                name: "IDX_d54a512b822fac7ed52800f6b4",
                table: "follow_request",
                newName: "IX_follow_request_followerId_followeeId");

            migrationBuilder.RenameIndex(
                name: "IDX_a7fd92dd6dc519e6fb435dd108",
                table: "follow_request",
                newName: "IX_follow_request_followerId");

            migrationBuilder.RenameIndex(
                name: "IDX_12c01c0d1a79f77d9f6c15fadd",
                table: "follow_request",
                newName: "IX_follow_request_followeeId");

            migrationBuilder.RenameIndex(
                name: "IDX_b37dafc86e9af007e3295c2781",
                table: "emoji",
                newName: "IX_emoji_name");

            migrationBuilder.RenameIndex(
                name: "IDX_5900e907bb46516ddf2871327c",
                table: "emoji",
                newName: "IX_emoji_host");

            migrationBuilder.RenameIndex(
                name: "IDX_4f4d35e1256c84ae3d1f0eab10",
                table: "emoji",
                newName: "IX_emoji_name_host");

            migrationBuilder.RenameIndex(
                name: "IDX_f4fc06e49c0171c85f1c48060d",
                table: "drive_folder",
                newName: "IX_drive_folder_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_02878d441ceae15ce060b73daf",
                table: "drive_folder",
                newName: "IX_drive_folder_createdAt");

            migrationBuilder.RenameIndex(
                name: "IDX_00ceffb0cdc238b3233294f08f",
                table: "drive_folder",
                newName: "IX_drive_folder_parentId");

            migrationBuilder.RenameIndex(
                name: "IDX_e74022ce9a074b3866f70e0d27",
                table: "drive_file",
                newName: "IX_drive_file_thumbnailAccessKey");

            migrationBuilder.RenameIndex(
                name: "IDX_e5848eac4940934e23dbc17581",
                table: "drive_file",
                newName: "IX_drive_file_uri");

            migrationBuilder.RenameIndex(
                name: "IDX_d85a184c2540d2deba33daf642",
                table: "drive_file",
                newName: "IX_drive_file_accessKey");

            migrationBuilder.RenameIndex(
                name: "IDX_c8dfad3b72196dd1d6b5db168a",
                table: "drive_file",
                newName: "IX_drive_file_createdAt");

            migrationBuilder.RenameIndex(
                name: "IDX_c55b2b7c284d9fef98026fc88e",
                table: "drive_file",
                newName: "IX_drive_file_webpublicAccessKey");

            migrationBuilder.RenameIndex(
                name: "IDX_bb90d1956dafc4068c28aa7560",
                table: "drive_file",
                newName: "IX_drive_file_folderId");

            migrationBuilder.RenameIndex(
                name: "IDX_a7eba67f8b3fa27271e85d2e26",
                table: "drive_file",
                newName: "IX_drive_file_isSensitive");

            migrationBuilder.RenameIndex(
                name: "IDX_a40b8df8c989d7db937ea27cf6",
                table: "drive_file",
                newName: "IX_drive_file_type");

            migrationBuilder.RenameIndex(
                name: "IDX_92779627994ac79277f070c91e",
                table: "drive_file",
                newName: "IX_drive_file_userHost");

            migrationBuilder.RenameIndex(
                name: "IDX_860fa6f6c7df5bb887249fba22",
                table: "drive_file",
                newName: "IX_drive_file_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_55720b33a61a7c806a8215b825",
                table: "drive_file",
                newName: "IX_drive_file_userId_folderId_id");

            migrationBuilder.RenameIndex(
                name: "IDX_37bb9a1b4585f8a3beb24c62d6",
                table: "drive_file",
                newName: "IX_drive_file_md5");

            migrationBuilder.RenameIndex(
                name: "IDX_315c779174fe8247ab324f036e",
                table: "drive_file",
                newName: "IX_drive_file_isLink");

            migrationBuilder.RenameIndex(
                name: "IDX_ebe99317bbbe9968a0c6f579ad",
                table: "clip_note",
                newName: "IX_clip_note_clipId");

            migrationBuilder.RenameIndex(
                name: "IDX_a012eaf5c87c65da1deb5fdbfa",
                table: "clip_note",
                newName: "IX_clip_note_noteId");

            migrationBuilder.RenameIndex(
                name: "IDX_6fc0ec357d55a18646262fdfff",
                table: "clip_note",
                newName: "IX_clip_note_noteId_clipId");

            migrationBuilder.RenameIndex(
                name: "IDX_2b5ec6c574d6802c94c80313fb",
                table: "clip",
                newName: "IX_clip_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_f36fed37d6d4cdcc68c803cd9c",
                table: "channel_note_pin",
                newName: "IX_channel_note_pin_channelId_noteId");

            migrationBuilder.RenameIndex(
                name: "IDX_8125f950afd3093acb10d2db8a",
                table: "channel_note_pin",
                newName: "IX_channel_note_pin_channelId");

            migrationBuilder.RenameIndex(
                name: "IDX_6d8084ec9496e7334a4602707e",
                table: "channel_following",
                newName: "IX_channel_following_followerId");

            migrationBuilder.RenameIndex(
                name: "IDX_2e230dd45a10e671d781d99f3e",
                table: "channel_following",
                newName: "IX_channel_following_followerId_followeeId");

            migrationBuilder.RenameIndex(
                name: "IDX_11e71f2511589dcc8a4d3214f9",
                table: "channel_following",
                newName: "IX_channel_following_createdAt");

            migrationBuilder.RenameIndex(
                name: "IDX_0e43068c3f92cab197c3d3cd86",
                table: "channel_following",
                newName: "IX_channel_following_followeeId");

            migrationBuilder.RenameIndex(
                name: "IDX_823bae55bd81b3be6e05cff438",
                table: "channel",
                newName: "IX_channel_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_71cb7b435b7c0d4843317e7e16",
                table: "channel",
                newName: "IX_channel_createdAt");

            migrationBuilder.RenameIndex(
                name: "IDX_29ef80c6f13bcea998447fce43",
                table: "channel",
                newName: "IX_channel_lastNotedAt");

            migrationBuilder.RenameIndex(
                name: "IDX_0f58c11241e649d2a638a8de94",
                table: "channel",
                newName: "IX_channel_notesCount");

            migrationBuilder.RenameIndex(
                name: "IDX_094b86cd36bb805d1aa1e8cc9a",
                table: "channel",
                newName: "IX_channel_usersCount");

            migrationBuilder.RenameIndex(
                name: "IDX_b9a354f7941c1e779f3b33aea6",
                table: "blocking",
                newName: "IX_blocking_createdAt");

            migrationBuilder.RenameIndex(
                name: "IDX_98a1bc5cb30dfd159de056549f",
                table: "blocking",
                newName: "IX_blocking_blockerId_blockeeId");

            migrationBuilder.RenameIndex(
                name: "IDX_2cd4a2743a99671308f5417759",
                table: "blocking",
                newName: "IX_blocking_blockeeId");

            migrationBuilder.RenameIndex(
                name: "IDX_0627125f1a8a42c9a1929edb55",
                table: "blocking",
                newName: "IX_blocking_blockerId");

            migrationBuilder.RenameIndex(
                name: "IDX_62cb09e1129f6ec024ef66e183",
                table: "auth_session",
                newName: "IX_auth_session_token");

            migrationBuilder.RenameIndex(
                name: "IDX_f1a461a618fa1755692d0e0d59",
                table: "attestation_challenge",
                newName: "IX_attestation_challenge_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_47efb914aed1f72dd39a306c7b",
                table: "attestation_challenge",
                newName: "IX_attestation_challenge_challenge");

            migrationBuilder.RenameIndex(
                name: "IDX_f49922d511d666848f250663c4",
                table: "app",
                newName: "IX_app_secret");

            migrationBuilder.RenameIndex(
                name: "IDX_3f5b0899ef90527a3462d7c2cb",
                table: "app",
                newName: "IX_app_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_048a757923ed8b157e9895da53",
                table: "app",
                newName: "IX_app_createdAt");

            migrationBuilder.RenameIndex(
                name: "IDX_6446c571a0e8d0f05f01c78909",
                table: "antenna",
                newName: "IX_antenna_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_924fa71815cfa3941d003702a0",
                table: "announcement_read",
                newName: "IX_announcement_read_userId_announcementId");

            migrationBuilder.RenameIndex(
                name: "IDX_8288151386172b8109f7239ab2",
                table: "announcement_read",
                newName: "IX_announcement_read_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_603a7b1e7aa0533c6c88e9bfaf",
                table: "announcement_read",
                newName: "IX_announcement_read_announcementId");

            migrationBuilder.RenameIndex(
                name: "IDX_118ec703e596086fc4515acb39",
                table: "announcement",
                newName: "IX_announcement_createdAt");

            migrationBuilder.RenameIndex(
                name: "IDX_bf3a053c07d9fb5d87317c56ee",
                table: "access_token",
                newName: "IX_access_token_session");

            migrationBuilder.RenameIndex(
                name: "IDX_9949557d0e1b2c19e5344c171e",
                table: "access_token",
                newName: "IX_access_token_userId");

            migrationBuilder.RenameIndex(
                name: "IDX_70ba8f6af34bc924fc9e12adb8",
                table: "access_token",
                newName: "IX_access_token_token");

            migrationBuilder.RenameIndex(
                name: "IDX_64c327441248bae40f7d92f34f",
                table: "access_token",
                newName: "IX_access_token_hash");

            migrationBuilder.RenameIndex(
                name: "IDX_f8d8b93740ad12c4ce8213a199",
                table: "abuse_user_report",
                newName: "IX_abuse_user_report_reporterHost");

            migrationBuilder.RenameIndex(
                name: "IDX_db2098070b2b5a523c58181f74",
                table: "abuse_user_report",
                newName: "IX_abuse_user_report_createdAt");

            migrationBuilder.RenameIndex(
                name: "IDX_a9021cc2e1feb5f72d3db6e9f5",
                table: "abuse_user_report",
                newName: "IX_abuse_user_report_targetUserId");

            migrationBuilder.RenameIndex(
                name: "IDX_4ebbf7f93cdc10e8d1ef2fc6cd",
                table: "abuse_user_report",
                newName: "IX_abuse_user_report_targetUserHost");

            migrationBuilder.RenameIndex(
                name: "IDX_2b15aaf4a0dc5be3499af7ab6a",
                table: "abuse_user_report",
                newName: "IX_abuse_user_report_resolved");

            migrationBuilder.RenameIndex(
                name: "IDX_04cc96756f89d0b7f9473e8cdf",
                table: "abuse_user_report",
                newName: "IX_abuse_user_report_reporterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_webhook_userId",
                table: "webhook",
                newName: "IDX_f272c8c8805969e6a6449c77b3");

            migrationBuilder.RenameIndex(
                name: "IX_webhook_on",
                table: "webhook",
                newName: "IDX_8063a0586ed1dfbe86e982d961");

            migrationBuilder.RenameIndex(
                name: "IX_webhook_active",
                table: "webhook",
                newName: "IDX_5a056076f76b2efe08216ba655");

            migrationBuilder.RenameIndex(
                name: "IX_user_security_key_userId",
                table: "user_security_key",
                newName: "IDX_ff9ca3b5f3ee3d0681367a9b44");

            migrationBuilder.RenameIndex(
                name: "IX_user_security_key_publicKey",
                table: "user_security_key",
                newName: "IDX_0d7718e562dcedd0aa5cf2c9f7");

            migrationBuilder.RenameIndex(
                name: "IX_user_publickey_keyId",
                table: "user_publickey",
                newName: "IDX_171e64971c780ebd23fae140bb");

            migrationBuilder.RenameIndex(
                name: "IX_user_profile_userHost",
                table: "user_profile",
                newName: "IDX_dce530b98e454793dac5ec2f5a");

            migrationBuilder.RenameIndex(
                name: "IX_user_profile_pinnedPageId",
                table: "user_profile",
                newName: "UQ_6dc44f1ceb65b1e72bacef2ca27");

            migrationBuilder.RenameIndex(
                name: "IX_user_profile_enableWordMute",
                table: "user_profile",
                newName: "IDX_3befe6f999c86aff06eb0257b4");

            migrationBuilder.RenameIndex(
                name: "IX_user_pending_code",
                table: "user_pending",
                newName: "IDX_4e5c4c99175638ec0761714ab0");

            migrationBuilder.RenameIndex(
                name: "IX_user_note_pin_userId_noteId",
                table: "user_note_pin",
                newName: "IDX_410cd649884b501c02d6e72738");

            migrationBuilder.RenameIndex(
                name: "IX_user_note_pin_userId",
                table: "user_note_pin",
                newName: "IDX_bfbc6f79ba4007b4ce5097f08d");

            migrationBuilder.RenameIndex(
                name: "IX_user_list_member_userListId",
                table: "user_list_member",
                newName: "IDX_605472305f26818cc93d1baaa7");

            migrationBuilder.RenameIndex(
                name: "IX_user_list_member_userId_userListId",
                table: "user_list_member",
                newName: "IDX_90f7da835e4c10aca6853621e1");

            migrationBuilder.RenameIndex(
                name: "IX_user_list_member_userId",
                table: "user_list_member",
                newName: "IDX_d844bfc6f3f523a05189076efa");

            migrationBuilder.RenameIndex(
                name: "IX_user_list_userId",
                table: "user_list",
                newName: "IDX_b7fcefbdd1c18dce86687531f9");

            migrationBuilder.RenameIndex(
                name: "IX_user_group_member_userId_userGroupId",
                table: "user_group_member",
                newName: "IDX_d9ecaed8c6dc43f3592c229282");

            migrationBuilder.RenameIndex(
                name: "IX_user_group_member_userId",
                table: "user_group_member",
                newName: "IDX_f3a1b4bd0c7cabba958a0c0b23");

            migrationBuilder.RenameIndex(
                name: "IX_user_group_member_userGroupId",
                table: "user_group_member",
                newName: "IDX_67dc758bc0566985d1b3d39986");

            migrationBuilder.RenameIndex(
                name: "IX_user_group_invite_userId_userGroupId",
                table: "user_group_invite",
                newName: "IDX_78787741f9010886796f2320a4");

            migrationBuilder.RenameIndex(
                name: "IX_user_group_invite_userId",
                table: "user_group_invite",
                newName: "IDX_1039988afa3bf991185b277fe0");

            migrationBuilder.RenameIndex(
                name: "IX_user_group_invite_userGroupId",
                table: "user_group_invite",
                newName: "IDX_e10924607d058004304611a436");

            migrationBuilder.RenameIndex(
                name: "IX_user_group_invitation_userId_userGroupId",
                table: "user_group_invitation",
                newName: "IDX_e9793f65f504e5a31fbaedbf2f");

            migrationBuilder.RenameIndex(
                name: "IX_user_group_invitation_userId",
                table: "user_group_invitation",
                newName: "IDX_bfbc6305547539369fe73eb144");

            migrationBuilder.RenameIndex(
                name: "IX_user_group_invitation_userGroupId",
                table: "user_group_invitation",
                newName: "IDX_5cc8c468090e129857e9fecce5");

            migrationBuilder.RenameIndex(
                name: "IX_user_group_userId",
                table: "user_group",
                newName: "IDX_3d6b372788ab01be58853003c9");

            migrationBuilder.RenameIndex(
                name: "IX_user_group_createdAt",
                table: "user_group",
                newName: "IDX_20e30aa35180e317e133d75316");

            migrationBuilder.RenameIndex(
                name: "IX_user_usernameLower_host",
                table: "user",
                newName: "IDX_5deb01ae162d1d70b80d064c27");

            migrationBuilder.RenameIndex(
                name: "IX_user_usernameLower",
                table: "user",
                newName: "IDX_a27b942a0d6dcff90e3ee9b5e8");

            migrationBuilder.RenameIndex(
                name: "IX_user_uri",
                table: "user",
                newName: "IDX_be623adaa4c566baf5d29ce0c8");

            migrationBuilder.RenameIndex(
                name: "IX_user_updatedAt",
                table: "user",
                newName: "IDX_80ca6e6ef65fb9ef34ea8c90f4");

            migrationBuilder.RenameIndex(
                name: "IX_user_token",
                table: "user",
                newName: "UQ_a854e557b1b14814750c7c7b0c9");

            migrationBuilder.RenameIndex(
                name: "IX_user_tags",
                table: "user",
                newName: "IDX_fa99d777623947a5b05f394cae");

            migrationBuilder.RenameIndex(
                name: "IX_user_lastActiveDate",
                table: "user",
                newName: "IDX_c8cc87bd0f2f4487d17c651fbf");

            migrationBuilder.RenameIndex(
                name: "IX_user_isExplorable",
                table: "user",
                newName: "IDX_d5a1b83c7cab66f167e6888188");

            migrationBuilder.RenameIndex(
                name: "IX_user_host",
                table: "user",
                newName: "IDX_3252a5df8d5bbd16b281f7799e");

            migrationBuilder.RenameIndex(
                name: "IX_user_createdAt",
                table: "user",
                newName: "IDX_e11e649824a45d8ed01d597fd9");

            migrationBuilder.RenameIndex(
                name: "IX_user_bannerId",
                table: "user",
                newName: "REL_afc64b53f8db3707ceb34eb28e");

            migrationBuilder.RenameIndex(
                name: "IX_user_avatarId",
                table: "user",
                newName: "REL_58f5c71eaab331645112cf8cfa");

            migrationBuilder.RenameIndex(
                name: "IX_sw_subscription_userId",
                table: "sw_subscription",
                newName: "IDX_97754ca6f2baff9b4abb7f853d");

            migrationBuilder.RenameIndex(
                name: "IX_signin_userId",
                table: "signin",
                newName: "IDX_2c308dbdc50d94dc625670055f");

            migrationBuilder.RenameIndex(
                name: "IX_session_token",
                table: "session",
                newName: "IDX_232f8e85d7633bd6ddfad42169");

            migrationBuilder.RenameIndex(
                name: "IX_renote_muting_muterId_muteeId",
                table: "renote_muting",
                newName: "IDX_0d801c609cec4e9eb4b6b4490c");

            migrationBuilder.RenameIndex(
                name: "IX_renote_muting_muterId",
                table: "renote_muting",
                newName: "IDX_7aa72a5fe76019bfe8e5e0e8b7");

            migrationBuilder.RenameIndex(
                name: "IX_renote_muting_muteeId",
                table: "renote_muting",
                newName: "IDX_7eac97594bcac5ffcf2068089b");

            migrationBuilder.RenameIndex(
                name: "IX_renote_muting_createdAt",
                table: "renote_muting",
                newName: "IDX_d1259a2c2b7bb413ff449e8711");

            migrationBuilder.RenameIndex(
                name: "IX_relay_inbox",
                table: "relay",
                newName: "IDX_0d9a1738f2cf7f3b1c3334dfab");

            migrationBuilder.RenameIndex(
                name: "IX_registry_item_userId",
                table: "registry_item",
                newName: "IDX_fb9d21ba0abb83223263df6bcb");

            migrationBuilder.RenameIndex(
                name: "IX_registry_item_scope",
                table: "registry_item",
                newName: "IDX_22baca135bb8a3ea1a83d13df3");

            migrationBuilder.RenameIndex(
                name: "IX_registry_item_domain",
                table: "registry_item",
                newName: "IDX_0a72bdfcdb97c0eca11fe7ecad");

            migrationBuilder.RenameIndex(
                name: "IX_registration_ticket_code",
                table: "registration_ticket",
                newName: "IDX_0ff69e8dfa9fe31bb4a4660f59");

            migrationBuilder.RenameIndex(
                name: "IX_promo_read_userId_noteId",
                table: "promo_read",
                newName: "IDX_2882b8a1a07c7d281a98b6db16");

            migrationBuilder.RenameIndex(
                name: "IX_promo_read_userId",
                table: "promo_read",
                newName: "IDX_9657d55550c3d37bfafaf7d4b0");

            migrationBuilder.RenameIndex(
                name: "IX_promo_note_userId",
                table: "promo_note",
                newName: "IDX_83f0862e9bae44af52ced7099e");

            migrationBuilder.RenameIndex(
                name: "IX_poll_vote_userId_noteId_choice",
                table: "poll_vote",
                newName: "IDX_50bd7164c5b78f1f4a42c4d21f");

            migrationBuilder.RenameIndex(
                name: "IX_poll_vote_userId",
                table: "poll_vote",
                newName: "IDX_66d2bd2ee31d14bcc23069a89f");

            migrationBuilder.RenameIndex(
                name: "IX_poll_vote_noteId",
                table: "poll_vote",
                newName: "IDX_aecfbd5ef60374918e63ee95fa");

            migrationBuilder.RenameIndex(
                name: "IX_poll_vote_createdAt",
                table: "poll_vote",
                newName: "IDX_0fb627e1c2f753262a74f0562d");

            migrationBuilder.RenameIndex(
                name: "IX_poll_userId",
                table: "poll",
                newName: "IDX_0610ebcfcfb4a18441a9bcdab2");

            migrationBuilder.RenameIndex(
                name: "IX_poll_userHost",
                table: "poll",
                newName: "IDX_7fa20a12319c7f6dc3aed98c0a");

            migrationBuilder.RenameIndex(
                name: "IX_password_reset_request_userId",
                table: "password_reset_request",
                newName: "IDX_4bb7fd4a34492ae0e6cc8d30ac");

            migrationBuilder.RenameIndex(
                name: "IX_password_reset_request_token",
                table: "password_reset_request",
                newName: "IDX_0b575fa9a4cfe638a925949285");

            migrationBuilder.RenameIndex(
                name: "IX_page_like_userId_pageId",
                table: "page_like",
                newName: "IDX_4ce6fb9c70529b4c8ac46c9bfa");

            migrationBuilder.RenameIndex(
                name: "IX_page_like_userId",
                table: "page_like",
                newName: "IDX_0e61efab7f88dbb79c9166dbb4");

            migrationBuilder.RenameIndex(
                name: "IX_page_visibleUserIds",
                table: "page",
                newName: "IDX_90148bbc2bf0854428786bfc15");

            migrationBuilder.RenameIndex(
                name: "IX_page_userId_name",
                table: "page",
                newName: "IDX_2133ef8317e4bdb839c0dcbf13");

            migrationBuilder.RenameIndex(
                name: "IX_page_userId",
                table: "page",
                newName: "IDX_ae1d917992dd0c9d9bbdad06c4");

            migrationBuilder.RenameIndex(
                name: "IX_page_updatedAt",
                table: "page",
                newName: "IDX_af639b066dfbca78b01a920f8a");

            migrationBuilder.RenameIndex(
                name: "IX_page_name",
                table: "page",
                newName: "IDX_b82c19c08afb292de4600d99e4");

            migrationBuilder.RenameIndex(
                name: "IX_page_createdAt",
                table: "page",
                newName: "IDX_fbb4297c927a9b85e9cefa2eb1");

            migrationBuilder.RenameIndex(
                name: "IX_oauth_token_token",
                table: "oauth_token",
                newName: "IDX_2cbeb4b389444bcf4379ef4273");

            migrationBuilder.RenameIndex(
                name: "IX_oauth_token_code",
                table: "oauth_token",
                newName: "IDX_dc5fe174a8b59025055f0ec136");

            migrationBuilder.RenameIndex(
                name: "IX_oauth_app_clientId",
                table: "oauth_app",
                newName: "IDX_65b61f406c811241e1315a2f82");

            migrationBuilder.RenameIndex(
                name: "IX_notification_type",
                table: "notification",
                newName: "IDX_33f33cc8ef29d805a97ff4628b");

            migrationBuilder.RenameIndex(
                name: "IX_notification_notifierId",
                table: "notification",
                newName: "IDX_3b4e96eec8d36a8bbb9d02aa71");

            migrationBuilder.RenameIndex(
                name: "IX_notification_notifieeId",
                table: "notification",
                newName: "IDX_3c601b70a1066d2c8b517094cb");

            migrationBuilder.RenameIndex(
                name: "IX_notification_isRead",
                table: "notification",
                newName: "IDX_080ab397c379af09b9d2169e5b");

            migrationBuilder.RenameIndex(
                name: "IX_notification_createdAt",
                table: "notification",
                newName: "IDX_b11a5e627c41d4dc3170f1d370");

            migrationBuilder.RenameIndex(
                name: "IX_notification_appAccessTokenId",
                table: "notification",
                newName: "IDX_e22bf6bda77b6adc1fd9e75c8c");

            migrationBuilder.RenameIndex(
                name: "IX_note_watching_userId_noteId",
                table: "note_watching",
                newName: "IDX_a42c93c69989ce1d09959df4cf");

            migrationBuilder.RenameIndex(
                name: "IX_note_watching_userId",
                table: "note_watching",
                newName: "IDX_b0134ec406e8d09a540f818288");

            migrationBuilder.RenameIndex(
                name: "IX_note_watching_noteUserId",
                table: "note_watching",
                newName: "IDX_44499765eec6b5489d72c4253b");

            migrationBuilder.RenameIndex(
                name: "IX_note_watching_noteId",
                table: "note_watching",
                newName: "IDX_03e7028ab8388a3f5e3ce2a861");

            migrationBuilder.RenameIndex(
                name: "IX_note_watching_createdAt",
                table: "note_watching",
                newName: "IDX_318cdf42a9cfc11f479bd802bb");

            migrationBuilder.RenameIndex(
                name: "IX_note_unread_userId_noteId",
                table: "note_unread",
                newName: "IDX_d908433a4953cc13216cd9c274");

            migrationBuilder.RenameIndex(
                name: "IX_note_unread_userId",
                table: "note_unread",
                newName: "IDX_56b0166d34ddae49d8ef7610bb");

            migrationBuilder.RenameIndex(
                name: "IX_note_unread_noteUserId",
                table: "note_unread",
                newName: "IDX_29e8c1d579af54d4232939f994");

            migrationBuilder.RenameIndex(
                name: "IX_note_unread_noteId",
                table: "note_unread",
                newName: "IDX_e637cba4dc4410218c4251260e");

            migrationBuilder.RenameIndex(
                name: "IX_note_unread_noteChannelId",
                table: "note_unread",
                newName: "IDX_6a57f051d82c6d4036c141e107");

            migrationBuilder.RenameIndex(
                name: "IX_note_unread_isSpecified",
                table: "note_unread",
                newName: "IDX_89a29c9237b8c3b6b3cbb4cb30");

            migrationBuilder.RenameIndex(
                name: "IX_note_unread_isMentioned",
                table: "note_unread",
                newName: "IDX_25b1dd384bec391b07b74b861c");

            migrationBuilder.RenameIndex(
                name: "IX_note_thread_muting_userId_threadId",
                table: "note_thread_muting",
                newName: "IDX_ae7aab18a2641d3e5f25e0c4ea");

            migrationBuilder.RenameIndex(
                name: "IX_note_thread_muting_userId",
                table: "note_thread_muting",
                newName: "IDX_29c11c7deb06615076f8c95b80");

            migrationBuilder.RenameIndex(
                name: "IX_note_thread_muting_threadId",
                table: "note_thread_muting",
                newName: "IDX_c426394644267453e76f036926");

            migrationBuilder.RenameIndex(
                name: "IX_note_reaction_userId_noteId",
                table: "note_reaction",
                newName: "IDX_ad0c221b25672daf2df320a817");

            migrationBuilder.RenameIndex(
                name: "IX_note_reaction_userId",
                table: "note_reaction",
                newName: "IDX_13761f64257f40c5636d0ff95e");

            migrationBuilder.RenameIndex(
                name: "IX_note_reaction_noteId",
                table: "note_reaction",
                newName: "IDX_45145e4953780f3cd5656f0ea6");

            migrationBuilder.RenameIndex(
                name: "IX_note_reaction_createdAt",
                table: "note_reaction",
                newName: "IDX_01f4581f114e0ebd2bbb876f0b");

            migrationBuilder.RenameIndex(
                name: "IX_note_favorite_userId_noteId",
                table: "note_favorite",
                newName: "IDX_0f4fb9ad355f3effff221ef245");

            migrationBuilder.RenameIndex(
                name: "IX_note_favorite_userId",
                table: "note_favorite",
                newName: "IDX_47f4b1892f5d6ba8efb3057d81");

            migrationBuilder.RenameIndex(
                name: "IX_note_edit_noteId",
                table: "note_edit",
                newName: "IDX_702ad5ae993a672e4fbffbcd38");

            migrationBuilder.RenameIndex(
                name: "IX_note_visibleUserIds",
                table: "note",
                newName: "IDX_796a8c03959361f97dc2be1d5c");

            migrationBuilder.RenameIndex(
                name: "IX_note_userId_id",
                table: "note",
                newName: "IDX_note_userId_id");

            migrationBuilder.RenameIndex(
                name: "IX_note_userId",
                table: "note",
                newName: "IDX_5b87d9d19127bd5d92026017a7");

            migrationBuilder.RenameIndex(
                name: "IX_note_userHost",
                table: "note",
                newName: "IDX_7125a826ab192eb27e11d358a5");

            migrationBuilder.RenameIndex(
                name: "IX_note_url",
                table: "note",
                newName: "IDX_note_url");

            migrationBuilder.RenameIndex(
                name: "IX_note_uri",
                table: "note",
                newName: "IDX_153536c67d05e9adb24e99fc2b");

            migrationBuilder.RenameIndex(
                name: "IX_note_threadId",
                table: "note",
                newName: "IDX_d4ebdef929896d6dc4a3c5bb48");

            migrationBuilder.RenameIndex(
                name: "IX_note_tags",
                table: "note",
                newName: "IDX_88937d94d7443d9a99a76fa5c0");

            migrationBuilder.RenameIndex(
                name: "IX_note_replyId",
                table: "note",
                newName: "IDX_17cb3553c700a4985dff5a30ff");

            migrationBuilder.RenameIndex(
                name: "IX_note_renoteId",
                table: "note",
                newName: "IDX_52ccc804d7c69037d558bac4c9");

            migrationBuilder.RenameIndex(
                name: "IX_note_mentions",
                table: "note",
                newName: "IDX_54ebcb6d27222913b908d56fd8");

            migrationBuilder.RenameIndex(
                name: "IX_note_id_userHost",
                table: "note",
                newName: "IDX_note_id_userHost");

            migrationBuilder.RenameIndex(
                name: "IX_note_fileIds",
                table: "note",
                newName: "IDX_51c063b6a133a9cb87145450f5");

            migrationBuilder.RenameIndex(
                name: "IX_note_createdAt_userId",
                table: "note",
                newName: "IDX_note_createdAt_userId");

            migrationBuilder.RenameIndex(
                name: "IX_note_createdAt",
                table: "note",
                newName: "IDX_e7c0567f5261063592f022e9b5");

            migrationBuilder.RenameIndex(
                name: "IX_note_channelId",
                table: "note",
                newName: "IDX_f22169eb10657bded6d875ac8f");

            migrationBuilder.RenameIndex(
                name: "IX_note_attachedFileTypes",
                table: "note",
                newName: "IDX_25dfc71b0369b003a4cd434d0b");

            migrationBuilder.RenameIndex(
                name: "IX_muting_muterId_muteeId",
                table: "muting",
                newName: "IDX_1eb9d9824a630321a29fd3b290");

            migrationBuilder.RenameIndex(
                name: "IX_muting_muterId",
                table: "muting",
                newName: "IDX_93060675b4a79a577f31d260c6");

            migrationBuilder.RenameIndex(
                name: "IX_muting_muteeId",
                table: "muting",
                newName: "IDX_ec96b4fed9dae517e0dbbe0675");

            migrationBuilder.RenameIndex(
                name: "IX_muting_expiresAt",
                table: "muting",
                newName: "IDX_c1fd1c3dfb0627aa36c253fd14");

            migrationBuilder.RenameIndex(
                name: "IX_muting_createdAt",
                table: "muting",
                newName: "IDX_f86d57fbca33c7a4e6897490cc");

            migrationBuilder.RenameIndex(
                name: "IX_moderation_log_userId",
                table: "moderation_log",
                newName: "IDX_a08ad074601d204e0f69da9a95");

            migrationBuilder.RenameIndex(
                name: "IX_messaging_message_userId",
                table: "messaging_message",
                newName: "IDX_5377c307783fce2b6d352e1203");

            migrationBuilder.RenameIndex(
                name: "IX_messaging_message_recipientId",
                table: "messaging_message",
                newName: "IDX_cac14a4e3944454a5ce7daa514");

            migrationBuilder.RenameIndex(
                name: "IX_messaging_message_groupId",
                table: "messaging_message",
                newName: "IDX_2c4be03b446884f9e9c502135b");

            migrationBuilder.RenameIndex(
                name: "IX_messaging_message_createdAt",
                table: "messaging_message",
                newName: "IDX_e21cd3646e52ef9c94aaf17c2e");

            migrationBuilder.RenameIndex(
                name: "IX_instance_isSuspended",
                table: "instance",
                newName: "IDX_34500da2e38ac393f7bb6b299c");

            migrationBuilder.RenameIndex(
                name: "IX_instance_host",
                table: "instance",
                newName: "IDX_8d5afc98982185799b160e10eb");

            migrationBuilder.RenameIndex(
                name: "IX_instance_caughtAt",
                table: "instance",
                newName: "IDX_2cd3b2a6b4cf0b910b260afe08");

            migrationBuilder.RenameIndex(
                name: "IX_hashtag_name",
                table: "hashtag",
                newName: "IDX_347fec870eafea7b26c8a73bac");

            migrationBuilder.RenameIndex(
                name: "IX_hashtag_mentionedUsersCount",
                table: "hashtag",
                newName: "IDX_2710a55f826ee236ea1a62698f");

            migrationBuilder.RenameIndex(
                name: "IX_hashtag_mentionedRemoteUsersCount",
                table: "hashtag",
                newName: "IDX_4c02d38a976c3ae132228c6fce");

            migrationBuilder.RenameIndex(
                name: "IX_hashtag_mentionedLocalUsersCount",
                table: "hashtag",
                newName: "IDX_0e206cec573f1edff4a3062923");

            migrationBuilder.RenameIndex(
                name: "IX_hashtag_attachedUsersCount",
                table: "hashtag",
                newName: "IDX_d57f9030cd3af7f63ffb1c267c");

            migrationBuilder.RenameIndex(
                name: "IX_hashtag_attachedRemoteUsersCount",
                table: "hashtag",
                newName: "IDX_0b03cbcd7e6a7ce068efa8ecc2");

            migrationBuilder.RenameIndex(
                name: "IX_hashtag_attachedLocalUsersCount",
                table: "hashtag",
                newName: "IDX_0c44bf4f680964145f2a68a341");

            migrationBuilder.RenameIndex(
                name: "IX_gallery_post_userId",
                table: "gallery_post",
                newName: "IDX_985b836dddd8615e432d7043dd");

            migrationBuilder.RenameIndex(
                name: "IX_gallery_post_updatedAt",
                table: "gallery_post",
                newName: "IDX_f631d37835adb04792e361807c");

            migrationBuilder.RenameIndex(
                name: "IX_gallery_post_tags",
                table: "gallery_post",
                newName: "IDX_05cca34b985d1b8edc1d1e28df");

            migrationBuilder.RenameIndex(
                name: "IX_gallery_post_likedCount",
                table: "gallery_post",
                newName: "IDX_1a165c68a49d08f11caffbd206");

            migrationBuilder.RenameIndex(
                name: "IX_gallery_post_isSensitive",
                table: "gallery_post",
                newName: "IDX_f2d744d9a14d0dfb8b96cb7fc5");

            migrationBuilder.RenameIndex(
                name: "IX_gallery_post_fileIds",
                table: "gallery_post",
                newName: "IDX_3ca50563facd913c425e7a89ee");

            migrationBuilder.RenameIndex(
                name: "IX_gallery_post_createdAt",
                table: "gallery_post",
                newName: "IDX_8f1a239bd077c8864a20c62c2c");

            migrationBuilder.RenameIndex(
                name: "IX_gallery_like_userId_postId",
                table: "gallery_like",
                newName: "IDX_df1b5f4099e99fb0bc5eae53b6");

            migrationBuilder.RenameIndex(
                name: "IX_gallery_like_userId",
                table: "gallery_like",
                newName: "IDX_8fd5215095473061855ceb948c");

            migrationBuilder.RenameIndex(
                name: "IX_following_followerId_followeeId",
                table: "following",
                newName: "IDX_307be5f1d1252e0388662acb96");

            migrationBuilder.RenameIndex(
                name: "IX_following_followerId",
                table: "following",
                newName: "IDX_6516c5a6f3c015b4eed39978be");

            migrationBuilder.RenameIndex(
                name: "IX_following_followerHost",
                table: "following",
                newName: "IDX_4ccd2239268ebbd1b35e318754");

            migrationBuilder.RenameIndex(
                name: "IX_following_followeeId",
                table: "following",
                newName: "IDX_24e0042143a18157b234df186c");

            migrationBuilder.RenameIndex(
                name: "IX_following_followeeHost",
                table: "following",
                newName: "IDX_fcdafee716dfe9c3b5fde90f30");

            migrationBuilder.RenameIndex(
                name: "IX_following_createdAt",
                table: "following",
                newName: "IDX_582f8fab771a9040a12961f3e7");

            migrationBuilder.RenameIndex(
                name: "IX_follow_request_followerId_followeeId",
                table: "follow_request",
                newName: "IDX_d54a512b822fac7ed52800f6b4");

            migrationBuilder.RenameIndex(
                name: "IX_follow_request_followerId",
                table: "follow_request",
                newName: "IDX_a7fd92dd6dc519e6fb435dd108");

            migrationBuilder.RenameIndex(
                name: "IX_follow_request_followeeId",
                table: "follow_request",
                newName: "IDX_12c01c0d1a79f77d9f6c15fadd");

            migrationBuilder.RenameIndex(
                name: "IX_emoji_name_host",
                table: "emoji",
                newName: "IDX_4f4d35e1256c84ae3d1f0eab10");

            migrationBuilder.RenameIndex(
                name: "IX_emoji_name",
                table: "emoji",
                newName: "IDX_b37dafc86e9af007e3295c2781");

            migrationBuilder.RenameIndex(
                name: "IX_emoji_host",
                table: "emoji",
                newName: "IDX_5900e907bb46516ddf2871327c");

            migrationBuilder.RenameIndex(
                name: "IX_drive_folder_userId",
                table: "drive_folder",
                newName: "IDX_f4fc06e49c0171c85f1c48060d");

            migrationBuilder.RenameIndex(
                name: "IX_drive_folder_parentId",
                table: "drive_folder",
                newName: "IDX_00ceffb0cdc238b3233294f08f");

            migrationBuilder.RenameIndex(
                name: "IX_drive_folder_createdAt",
                table: "drive_folder",
                newName: "IDX_02878d441ceae15ce060b73daf");

            migrationBuilder.RenameIndex(
                name: "IX_drive_file_webpublicAccessKey",
                table: "drive_file",
                newName: "IDX_c55b2b7c284d9fef98026fc88e");

            migrationBuilder.RenameIndex(
                name: "IX_drive_file_userId_folderId_id",
                table: "drive_file",
                newName: "IDX_55720b33a61a7c806a8215b825");

            migrationBuilder.RenameIndex(
                name: "IX_drive_file_userId",
                table: "drive_file",
                newName: "IDX_860fa6f6c7df5bb887249fba22");

            migrationBuilder.RenameIndex(
                name: "IX_drive_file_userHost",
                table: "drive_file",
                newName: "IDX_92779627994ac79277f070c91e");

            migrationBuilder.RenameIndex(
                name: "IX_drive_file_uri",
                table: "drive_file",
                newName: "IDX_e5848eac4940934e23dbc17581");

            migrationBuilder.RenameIndex(
                name: "IX_drive_file_type",
                table: "drive_file",
                newName: "IDX_a40b8df8c989d7db937ea27cf6");

            migrationBuilder.RenameIndex(
                name: "IX_drive_file_thumbnailAccessKey",
                table: "drive_file",
                newName: "IDX_e74022ce9a074b3866f70e0d27");

            migrationBuilder.RenameIndex(
                name: "IX_drive_file_md5",
                table: "drive_file",
                newName: "IDX_37bb9a1b4585f8a3beb24c62d6");

            migrationBuilder.RenameIndex(
                name: "IX_drive_file_isSensitive",
                table: "drive_file",
                newName: "IDX_a7eba67f8b3fa27271e85d2e26");

            migrationBuilder.RenameIndex(
                name: "IX_drive_file_isLink",
                table: "drive_file",
                newName: "IDX_315c779174fe8247ab324f036e");

            migrationBuilder.RenameIndex(
                name: "IX_drive_file_folderId",
                table: "drive_file",
                newName: "IDX_bb90d1956dafc4068c28aa7560");

            migrationBuilder.RenameIndex(
                name: "IX_drive_file_createdAt",
                table: "drive_file",
                newName: "IDX_c8dfad3b72196dd1d6b5db168a");

            migrationBuilder.RenameIndex(
                name: "IX_drive_file_accessKey",
                table: "drive_file",
                newName: "IDX_d85a184c2540d2deba33daf642");

            migrationBuilder.RenameIndex(
                name: "IX_clip_note_noteId_clipId",
                table: "clip_note",
                newName: "IDX_6fc0ec357d55a18646262fdfff");

            migrationBuilder.RenameIndex(
                name: "IX_clip_note_noteId",
                table: "clip_note",
                newName: "IDX_a012eaf5c87c65da1deb5fdbfa");

            migrationBuilder.RenameIndex(
                name: "IX_clip_note_clipId",
                table: "clip_note",
                newName: "IDX_ebe99317bbbe9968a0c6f579ad");

            migrationBuilder.RenameIndex(
                name: "IX_clip_userId",
                table: "clip",
                newName: "IDX_2b5ec6c574d6802c94c80313fb");

            migrationBuilder.RenameIndex(
                name: "IX_channel_note_pin_channelId_noteId",
                table: "channel_note_pin",
                newName: "IDX_f36fed37d6d4cdcc68c803cd9c");

            migrationBuilder.RenameIndex(
                name: "IX_channel_note_pin_channelId",
                table: "channel_note_pin",
                newName: "IDX_8125f950afd3093acb10d2db8a");

            migrationBuilder.RenameIndex(
                name: "IX_channel_following_followerId_followeeId",
                table: "channel_following",
                newName: "IDX_2e230dd45a10e671d781d99f3e");

            migrationBuilder.RenameIndex(
                name: "IX_channel_following_followerId",
                table: "channel_following",
                newName: "IDX_6d8084ec9496e7334a4602707e");

            migrationBuilder.RenameIndex(
                name: "IX_channel_following_followeeId",
                table: "channel_following",
                newName: "IDX_0e43068c3f92cab197c3d3cd86");

            migrationBuilder.RenameIndex(
                name: "IX_channel_following_createdAt",
                table: "channel_following",
                newName: "IDX_11e71f2511589dcc8a4d3214f9");

            migrationBuilder.RenameIndex(
                name: "IX_channel_usersCount",
                table: "channel",
                newName: "IDX_094b86cd36bb805d1aa1e8cc9a");

            migrationBuilder.RenameIndex(
                name: "IX_channel_userId",
                table: "channel",
                newName: "IDX_823bae55bd81b3be6e05cff438");

            migrationBuilder.RenameIndex(
                name: "IX_channel_notesCount",
                table: "channel",
                newName: "IDX_0f58c11241e649d2a638a8de94");

            migrationBuilder.RenameIndex(
                name: "IX_channel_lastNotedAt",
                table: "channel",
                newName: "IDX_29ef80c6f13bcea998447fce43");

            migrationBuilder.RenameIndex(
                name: "IX_channel_createdAt",
                table: "channel",
                newName: "IDX_71cb7b435b7c0d4843317e7e16");

            migrationBuilder.RenameIndex(
                name: "IX_blocking_createdAt",
                table: "blocking",
                newName: "IDX_b9a354f7941c1e779f3b33aea6");

            migrationBuilder.RenameIndex(
                name: "IX_blocking_blockerId_blockeeId",
                table: "blocking",
                newName: "IDX_98a1bc5cb30dfd159de056549f");

            migrationBuilder.RenameIndex(
                name: "IX_blocking_blockerId",
                table: "blocking",
                newName: "IDX_0627125f1a8a42c9a1929edb55");

            migrationBuilder.RenameIndex(
                name: "IX_blocking_blockeeId",
                table: "blocking",
                newName: "IDX_2cd4a2743a99671308f5417759");

            migrationBuilder.RenameIndex(
                name: "IX_auth_session_token",
                table: "auth_session",
                newName: "IDX_62cb09e1129f6ec024ef66e183");

            migrationBuilder.RenameIndex(
                name: "IX_attestation_challenge_userId",
                table: "attestation_challenge",
                newName: "IDX_f1a461a618fa1755692d0e0d59");

            migrationBuilder.RenameIndex(
                name: "IX_attestation_challenge_challenge",
                table: "attestation_challenge",
                newName: "IDX_47efb914aed1f72dd39a306c7b");

            migrationBuilder.RenameIndex(
                name: "IX_app_userId",
                table: "app",
                newName: "IDX_3f5b0899ef90527a3462d7c2cb");

            migrationBuilder.RenameIndex(
                name: "IX_app_secret",
                table: "app",
                newName: "IDX_f49922d511d666848f250663c4");

            migrationBuilder.RenameIndex(
                name: "IX_app_createdAt",
                table: "app",
                newName: "IDX_048a757923ed8b157e9895da53");

            migrationBuilder.RenameIndex(
                name: "IX_antenna_userId",
                table: "antenna",
                newName: "IDX_6446c571a0e8d0f05f01c78909");

            migrationBuilder.RenameIndex(
                name: "IX_announcement_read_userId_announcementId",
                table: "announcement_read",
                newName: "IDX_924fa71815cfa3941d003702a0");

            migrationBuilder.RenameIndex(
                name: "IX_announcement_read_userId",
                table: "announcement_read",
                newName: "IDX_8288151386172b8109f7239ab2");

            migrationBuilder.RenameIndex(
                name: "IX_announcement_read_announcementId",
                table: "announcement_read",
                newName: "IDX_603a7b1e7aa0533c6c88e9bfaf");

            migrationBuilder.RenameIndex(
                name: "IX_announcement_createdAt",
                table: "announcement",
                newName: "IDX_118ec703e596086fc4515acb39");

            migrationBuilder.RenameIndex(
                name: "IX_access_token_userId",
                table: "access_token",
                newName: "IDX_9949557d0e1b2c19e5344c171e");

            migrationBuilder.RenameIndex(
                name: "IX_access_token_token",
                table: "access_token",
                newName: "IDX_70ba8f6af34bc924fc9e12adb8");

            migrationBuilder.RenameIndex(
                name: "IX_access_token_session",
                table: "access_token",
                newName: "IDX_bf3a053c07d9fb5d87317c56ee");

            migrationBuilder.RenameIndex(
                name: "IX_access_token_hash",
                table: "access_token",
                newName: "IDX_64c327441248bae40f7d92f34f");

            migrationBuilder.RenameIndex(
                name: "IX_abuse_user_report_targetUserId",
                table: "abuse_user_report",
                newName: "IDX_a9021cc2e1feb5f72d3db6e9f5");

            migrationBuilder.RenameIndex(
                name: "IX_abuse_user_report_targetUserHost",
                table: "abuse_user_report",
                newName: "IDX_4ebbf7f93cdc10e8d1ef2fc6cd");

            migrationBuilder.RenameIndex(
                name: "IX_abuse_user_report_resolved",
                table: "abuse_user_report",
                newName: "IDX_2b15aaf4a0dc5be3499af7ab6a");

            migrationBuilder.RenameIndex(
                name: "IX_abuse_user_report_reporterId",
                table: "abuse_user_report",
                newName: "IDX_04cc96756f89d0b7f9473e8cdf");

            migrationBuilder.RenameIndex(
                name: "IX_abuse_user_report_reporterHost",
                table: "abuse_user_report",
                newName: "IDX_f8d8b93740ad12c4ce8213a199");

            migrationBuilder.RenameIndex(
                name: "IX_abuse_user_report_createdAt",
                table: "abuse_user_report",
                newName: "IDX_db2098070b2b5a523c58181f74");

            migrationBuilder.CreateIndex(
                name: "IDX_a854e557b1b14814750c7c7b0c",
                table: "user",
                column: "token",
                unique: true);
        }
    }
}
