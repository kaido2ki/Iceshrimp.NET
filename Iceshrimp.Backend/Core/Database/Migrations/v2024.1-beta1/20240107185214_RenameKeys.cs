using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240107185214_RenameKeys")]
    public partial class RenameKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_04cc96756f89d0b7f9473e8cdf3",
                table: "abuse_user_report");

            migrationBuilder.DropForeignKey(
                name: "FK_08b883dd5fdd6f9c4c1572b36de",
                table: "abuse_user_report");

            migrationBuilder.DropForeignKey(
                name: "FK_a9021cc2e1feb5f72d3db6e9f5f",
                table: "abuse_user_report");

            migrationBuilder.DropForeignKey(
                name: "FK_9949557d0e1b2c19e5344c171e9",
                table: "access_token");

            migrationBuilder.DropForeignKey(
                name: "FK_a3ff16c90cc87a82a0b5959e560",
                table: "access_token");

            migrationBuilder.DropForeignKey(
                name: "FK_603a7b1e7aa0533c6c88e9bfafe",
                table: "announcement_read");

            migrationBuilder.DropForeignKey(
                name: "FK_8288151386172b8109f7239ab28",
                table: "announcement_read");

            migrationBuilder.DropForeignKey(
                name: "FK_6446c571a0e8d0f05f01c789096",
                table: "antenna");

            migrationBuilder.DropForeignKey(
                name: "FK_709d7d32053d0dd7620f678eeb9",
                table: "antenna");

            migrationBuilder.DropForeignKey(
                name: "FK_ccbf5a8c0be4511133dcc50ddeb",
                table: "antenna");

            migrationBuilder.DropForeignKey(
                name: "FK_3f5b0899ef90527a3462d7c2cb3",
                table: "app");

            migrationBuilder.DropForeignKey(
                name: "FK_f1a461a618fa1755692d0e0d592",
                table: "attestation_challenge");

            migrationBuilder.DropForeignKey(
                name: "FK_c072b729d71697f959bde66ade0",
                table: "auth_session");

            migrationBuilder.DropForeignKey(
                name: "FK_dbe037d4bddd17b03a1dc778dee",
                table: "auth_session");

            migrationBuilder.DropForeignKey(
                name: "FK_0627125f1a8a42c9a1929edb552",
                table: "blocking");

            migrationBuilder.DropForeignKey(
                name: "FK_2cd4a2743a99671308f5417759e",
                table: "blocking");

            migrationBuilder.DropForeignKey(
                name: "FK_823bae55bd81b3be6e05cff4383",
                table: "channel");

            migrationBuilder.DropForeignKey(
                name: "FK_999da2bcc7efadbfe0e92d3bc19",
                table: "channel");

            migrationBuilder.DropForeignKey(
                name: "FK_0e43068c3f92cab197c3d3cd86e",
                table: "channel_following");

            migrationBuilder.DropForeignKey(
                name: "FK_6d8084ec9496e7334a4602707e1",
                table: "channel_following");

            migrationBuilder.DropForeignKey(
                name: "FK_10b19ef67d297ea9de325cd4502",
                table: "channel_note_pin");

            migrationBuilder.DropForeignKey(
                name: "FK_8125f950afd3093acb10d2db8a8",
                table: "channel_note_pin");

            migrationBuilder.DropForeignKey(
                name: "FK_2b5ec6c574d6802c94c80313fb2",
                table: "clip");

            migrationBuilder.DropForeignKey(
                name: "FK_a012eaf5c87c65da1deb5fdbfa3",
                table: "clip_note");

            migrationBuilder.DropForeignKey(
                name: "FK_ebe99317bbbe9968a0c6f579adf",
                table: "clip_note");

            migrationBuilder.DropForeignKey(
                name: "FK_860fa6f6c7df5bb887249fba22e",
                table: "drive_file");

            migrationBuilder.DropForeignKey(
                name: "FK_bb90d1956dafc4068c28aa7560a",
                table: "drive_file");

            migrationBuilder.DropForeignKey(
                name: "FK_00ceffb0cdc238b3233294f08f2",
                table: "drive_folder");

            migrationBuilder.DropForeignKey(
                name: "FK_f4fc06e49c0171c85f1c48060d2",
                table: "drive_folder");

            migrationBuilder.DropForeignKey(
                name: "FK_12c01c0d1a79f77d9f6c15fadd2",
                table: "follow_request");

            migrationBuilder.DropForeignKey(
                name: "FK_a7fd92dd6dc519e6fb435dd108f",
                table: "follow_request");

            migrationBuilder.DropForeignKey(
                name: "FK_24e0042143a18157b234df186c3",
                table: "following");

            migrationBuilder.DropForeignKey(
                name: "FK_6516c5a6f3c015b4eed39978be5",
                table: "following");

            migrationBuilder.DropForeignKey(
                name: "FK_8fd5215095473061855ceb948cf",
                table: "gallery_like");

            migrationBuilder.DropForeignKey(
                name: "FK_b1cb568bfe569e47b7051699fc8",
                table: "gallery_like");

            migrationBuilder.DropForeignKey(
                name: "FK_985b836dddd8615e432d7043ddb",
                table: "gallery_post");

            migrationBuilder.DropForeignKey(
                name: "FK_6ef86ec901b2017cbe82d3a8286",
                table: "html_note_cache_entry");

            migrationBuilder.DropForeignKey(
                name: "FK_920b9474e3c9cae3f3c37c057e1",
                table: "html_user_cache_entry");

            migrationBuilder.DropForeignKey(
                name: "FK_2c4be03b446884f9e9c502135be",
                table: "messaging_message");

            migrationBuilder.DropForeignKey(
                name: "FK_535def119223ac05ad3fa9ef64b",
                table: "messaging_message");

            migrationBuilder.DropForeignKey(
                name: "FK_5377c307783fce2b6d352e1203b",
                table: "messaging_message");

            migrationBuilder.DropForeignKey(
                name: "FK_cac14a4e3944454a5ce7daa5142",
                table: "messaging_message");

            migrationBuilder.DropForeignKey(
                name: "FK_a08ad074601d204e0f69da9a954",
                table: "moderation_log");

            migrationBuilder.DropForeignKey(
                name: "FK_93060675b4a79a577f31d260c67",
                table: "muting");

            migrationBuilder.DropForeignKey(
                name: "FK_ec96b4fed9dae517e0dbbe0675c",
                table: "muting");

            migrationBuilder.DropForeignKey(
                name: "FK_17cb3553c700a4985dff5a30ff5",
                table: "note");

            migrationBuilder.DropForeignKey(
                name: "FK_52ccc804d7c69037d558bac4c96",
                table: "note");

            migrationBuilder.DropForeignKey(
                name: "FK_5b87d9d19127bd5d92026017a7b",
                table: "note");

            migrationBuilder.DropForeignKey(
                name: "FK_f22169eb10657bded6d875ac8f9",
                table: "note");

            migrationBuilder.DropForeignKey(
                name: "FK_702ad5ae993a672e4fbffbcd38c",
                table: "note_edit");

            migrationBuilder.DropForeignKey(
                name: "FK_0e00498f180193423c992bc4370",
                table: "note_favorite");

            migrationBuilder.DropForeignKey(
                name: "FK_47f4b1892f5d6ba8efb3057d81a",
                table: "note_favorite");

            migrationBuilder.DropForeignKey(
                name: "FK_13761f64257f40c5636d0ff95ee",
                table: "note_reaction");

            migrationBuilder.DropForeignKey(
                name: "FK_45145e4953780f3cd5656f0ea6a",
                table: "note_reaction");

            migrationBuilder.DropForeignKey(
                name: "FK_29c11c7deb06615076f8c95b80a",
                table: "note_thread_muting");

            migrationBuilder.DropForeignKey(
                name: "FK_56b0166d34ddae49d8ef7610bb9",
                table: "note_unread");

            migrationBuilder.DropForeignKey(
                name: "FK_e637cba4dc4410218c4251260e4",
                table: "note_unread");

            migrationBuilder.DropForeignKey(
                name: "FK_03e7028ab8388a3f5e3ce2a8619",
                table: "note_watching");

            migrationBuilder.DropForeignKey(
                name: "FK_b0134ec406e8d09a540f8182888",
                table: "note_watching");

            migrationBuilder.DropForeignKey(
                name: "FK_3b4e96eec8d36a8bbb9d02aa710",
                table: "notification");

            migrationBuilder.DropForeignKey(
                name: "FK_3c601b70a1066d2c8b517094cb9",
                table: "notification");

            migrationBuilder.DropForeignKey(
                name: "FK_769cb6b73a1efe22ddf733ac453",
                table: "notification");

            migrationBuilder.DropForeignKey(
                name: "FK_8fe87814e978053a53b1beb7e98",
                table: "notification");

            migrationBuilder.DropForeignKey(
                name: "FK_bd7fab507621e635b32cd31892c",
                table: "notification");

            migrationBuilder.DropForeignKey(
                name: "FK_e22bf6bda77b6adc1fd9e75c8c9",
                table: "notification");

            migrationBuilder.DropForeignKey(
                name: "FK_6d3ef28ea647b1449ba79690874",
                table: "oauth_token");

            migrationBuilder.DropForeignKey(
                name: "FK_f6b4b1ac66b753feab5d831ba04",
                table: "oauth_token");

            migrationBuilder.DropForeignKey(
                name: "FK_a9ca79ad939bf06066b81c9d3aa",
                table: "page");

            migrationBuilder.DropForeignKey(
                name: "FK_ae1d917992dd0c9d9bbdad06c4a",
                table: "page");

            migrationBuilder.DropForeignKey(
                name: "FK_0e61efab7f88dbb79c9166dbb48",
                table: "page_like");

            migrationBuilder.DropForeignKey(
                name: "FK_cf8782626dced3176038176a847",
                table: "page_like");

            migrationBuilder.DropForeignKey(
                name: "FK_4bb7fd4a34492ae0e6cc8d30ac8",
                table: "password_reset_request");

            migrationBuilder.DropForeignKey(
                name: "FK_da851e06d0dfe2ef397d8b1bf1b",
                table: "poll");

            migrationBuilder.DropForeignKey(
                name: "FK_66d2bd2ee31d14bcc23069a89f8",
                table: "poll_vote");

            migrationBuilder.DropForeignKey(
                name: "FK_aecfbd5ef60374918e63ee95fa7",
                table: "poll_vote");

            migrationBuilder.DropForeignKey(
                name: "FK_e263909ca4fe5d57f8d4230dd5c",
                table: "promo_note");

            migrationBuilder.DropForeignKey(
                name: "FK_9657d55550c3d37bfafaf7d4b05",
                table: "promo_read");

            migrationBuilder.DropForeignKey(
                name: "FK_a46a1a603ecee695d7db26da5f4",
                table: "promo_read");

            migrationBuilder.DropForeignKey(
                name: "FK_fb9d21ba0abb83223263df6bcb3",
                table: "registry_item");

            migrationBuilder.DropForeignKey(
                name: "FK_7aa72a5fe76019bfe8e5e0e8b7d",
                table: "renote_muting");

            migrationBuilder.DropForeignKey(
                name: "FK_7eac97594bcac5ffcf2068089b6",
                table: "renote_muting");

            migrationBuilder.DropForeignKey(
                name: "FK_3d2f174ef04fb312fdebd0ddc53",
                table: "session");

            migrationBuilder.DropForeignKey(
                name: "FK_2c308dbdc50d94dc625670055f7",
                table: "signin");

            migrationBuilder.DropForeignKey(
                name: "FK_97754ca6f2baff9b4abb7f853dd",
                table: "sw_subscription");

            migrationBuilder.DropForeignKey(
                name: "FK_58f5c71eaab331645112cf8cfa5",
                table: "user");

            migrationBuilder.DropForeignKey(
                name: "FK_afc64b53f8db3707ceb34eb28e2",
                table: "user");

            migrationBuilder.DropForeignKey(
                name: "FK_3d6b372788ab01be58853003c93",
                table: "user_group");

            migrationBuilder.DropForeignKey(
                name: "FK_5cc8c468090e129857e9fecce5a",
                table: "user_group_invitation");

            migrationBuilder.DropForeignKey(
                name: "FK_bfbc6305547539369fe73eb144a",
                table: "user_group_invitation");

            migrationBuilder.DropForeignKey(
                name: "FK_1039988afa3bf991185b277fe03",
                table: "user_group_invite");

            migrationBuilder.DropForeignKey(
                name: "FK_e10924607d058004304611a436a",
                table: "user_group_invite");

            migrationBuilder.DropForeignKey(
                name: "FK_67dc758bc0566985d1b3d399865",
                table: "user_group_member");

            migrationBuilder.DropForeignKey(
                name: "FK_f3a1b4bd0c7cabba958a0c0b231",
                table: "user_group_member");

            migrationBuilder.DropForeignKey(
                name: "FK_f4853eb41ab722fe05f81cedeb6",
                table: "user_keypair");

            migrationBuilder.DropForeignKey(
                name: "FK_b7fcefbdd1c18dce86687531f99",
                table: "user_list");

            migrationBuilder.DropForeignKey(
                name: "FK_605472305f26818cc93d1baaa74",
                table: "user_list_member");

            migrationBuilder.DropForeignKey(
                name: "FK_d844bfc6f3f523a05189076efaa",
                table: "user_list_member");

            migrationBuilder.DropForeignKey(
                name: "FK_68881008f7c3588ad7ecae471cf",
                table: "user_note_pin");

            migrationBuilder.DropForeignKey(
                name: "FK_bfbc6f79ba4007b4ce5097f08d6",
                table: "user_note_pin");

            migrationBuilder.DropForeignKey(
                name: "FK_51cb79b5555effaf7d69ba1cff9",
                table: "user_profile");

            migrationBuilder.DropForeignKey(
                name: "FK_6dc44f1ceb65b1e72bacef2ca27",
                table: "user_profile");

            migrationBuilder.DropForeignKey(
                name: "FK_10c146e4b39b443ede016f6736d",
                table: "user_publickey");

            migrationBuilder.DropForeignKey(
                name: "FK_ff9ca3b5f3ee3d0681367a9b447",
                table: "user_security_key");

            migrationBuilder.DropForeignKey(
                name: "FK_f272c8c8805969e6a6449c77b3c",
                table: "webhook");

            migrationBuilder.DropPrimaryKey(
                name: "PK_e6765510c2d078db49632b59020",
                table: "webhook");

            migrationBuilder.DropPrimaryKey(
                name: "PK_3e508571121ab39c5f85d10c166",
                table: "user_security_key");

            migrationBuilder.DropPrimaryKey(
                name: "PK_10c146e4b39b443ede016f6736d",
                table: "user_publickey");

            migrationBuilder.DropPrimaryKey(
                name: "PK_51cb79b5555effaf7d69ba1cff9",
                table: "user_profile");

            migrationBuilder.DropPrimaryKey(
                name: "PK_d4c84e013c98ec02d19b8fbbafa",
                table: "user_pending");

            migrationBuilder.DropPrimaryKey(
                name: "PK_a6a2dad4ae000abce2ea9d9b103",
                table: "user_note_pin");

            migrationBuilder.DropPrimaryKey(
                name: "PK_11abb3768da1c5f8de101c9df45",
                table: "user_list_member");

            migrationBuilder.DropPrimaryKey(
                name: "PK_87bab75775fd9b1ff822b656402",
                table: "user_list");

            migrationBuilder.DropPrimaryKey(
                name: "PK_f4853eb41ab722fe05f81cedeb6",
                table: "user_keypair");

            migrationBuilder.DropPrimaryKey(
                name: "PK_15f2425885253c5507e1599cfe7",
                table: "user_group_member");

            migrationBuilder.DropPrimaryKey(
                name: "PK_3893884af0d3a5f4d01e7921a97",
                table: "user_group_invite");

            migrationBuilder.DropPrimaryKey(
                name: "PK_160c63ec02bf23f6a5c5e8140d6",
                table: "user_group_invitation");

            migrationBuilder.DropPrimaryKey(
                name: "PK_3c29fba6fe013ec8724378ce7c9",
                table: "user_group");

            migrationBuilder.DropPrimaryKey(
                name: "PK_cace4a159ff9f2512dd42373760",
                table: "user");

            migrationBuilder.DropPrimaryKey(
                name: "PK_78fd79d2d24c6ac2f4cc9a31a5d",
                table: "used_username");

            migrationBuilder.DropPrimaryKey(
                name: "PK_e8f763631530051b95eb6279b91",
                table: "sw_subscription");

            migrationBuilder.DropPrimaryKey(
                name: "PK_9e96ddc025712616fc492b3b588",
                table: "signin");

            migrationBuilder.DropPrimaryKey(
                name: "PK_f55da76ac1c3ac420f444d2ff11",
                table: "session");

            migrationBuilder.DropPrimaryKey(
                name: "PK_renoteMuting_id",
                table: "renote_muting");

            migrationBuilder.DropPrimaryKey(
                name: "PK_78ebc9cfddf4292633b7ba57aee",
                table: "relay");

            migrationBuilder.DropPrimaryKey(
                name: "PK_64b3f7e6008b4d89b826cd3af95",
                table: "registry_item");

            migrationBuilder.DropPrimaryKey(
                name: "PK_f11696b6fafcf3662d4292734f8",
                table: "registration_ticket");

            migrationBuilder.DropPrimaryKey(
                name: "PK_61917c1541002422b703318b7c9",
                table: "promo_read");

            migrationBuilder.DropPrimaryKey(
                name: "PK_e263909ca4fe5d57f8d4230dd5c",
                table: "promo_note");

            migrationBuilder.DropPrimaryKey(
                name: "PK_fd002d371201c472490ba89c6a0",
                table: "poll_vote");

            migrationBuilder.DropPrimaryKey(
                name: "PK_da851e06d0dfe2ef397d8b1bf1b",
                table: "poll");

            migrationBuilder.DropPrimaryKey(
                name: "PK_fcf4b02eae1403a2edaf87fd074",
                table: "password_reset_request");

            migrationBuilder.DropPrimaryKey(
                name: "PK_813f034843af992d3ae0f43c64c",
                table: "page_like");

            migrationBuilder.DropPrimaryKey(
                name: "PK_742f4117e065c5b6ad21b37ba1f",
                table: "page");

            migrationBuilder.DropPrimaryKey(
                name: "PK_7e6a25a3cc4395d1658f5b89c73",
                table: "oauth_token");

            migrationBuilder.DropPrimaryKey(
                name: "PK_3256b97c0a3ee2d67240805dca4",
                table: "oauth_app");

            migrationBuilder.DropPrimaryKey(
                name: "PK_705b6c7cdf9b2c2ff7ac7872cb7",
                table: "notification");

            migrationBuilder.DropPrimaryKey(
                name: "PK_49286fdb23725945a74aa27d757",
                table: "note_watching");

            migrationBuilder.DropPrimaryKey(
                name: "PK_1904eda61a784f57e6e51fa9c1f",
                table: "note_unread");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ec5936d94d1a0369646d12a3a47",
                table: "note_thread_muting");

            migrationBuilder.DropPrimaryKey(
                name: "PK_767ec729b108799b587a3fcc9cf",
                table: "note_reaction");

            migrationBuilder.DropPrimaryKey(
                name: "PK_af0da35a60b9fa4463a62082b36",
                table: "note_favorite");

            migrationBuilder.DropPrimaryKey(
                name: "PK_736fc6e0d4e222ecc6f82058e08",
                table: "note_edit");

            migrationBuilder.DropPrimaryKey(
                name: "PK_96d0c172a4fba276b1bbed43058",
                table: "note");

            migrationBuilder.DropPrimaryKey(
                name: "PK_2e92d06c8b5c602eeb27ca9ba48",
                table: "muting");

            migrationBuilder.DropPrimaryKey(
                name: "PK_d0adca6ecfd068db83e4526cc26",
                table: "moderation_log");

            migrationBuilder.DropPrimaryKey(
                name: "PK_c4c17a6c2bd7651338b60fc590b",
                table: "meta");

            migrationBuilder.DropPrimaryKey(
                name: "PK_db398fd79dc95d0eb8c30456eaa",
                table: "messaging_message");

            migrationBuilder.DropPrimaryKey(
                name: "PK_eaf60e4a0c399c9935413e06474",
                table: "instance");

            migrationBuilder.DropPrimaryKey(
                name: "PK_920b9474e3c9cae3f3c37c057e1",
                table: "html_user_cache_entry");

            migrationBuilder.DropPrimaryKey(
                name: "PK_6ef86ec901b2017cbe82d3a8286",
                table: "html_note_cache_entry");

            migrationBuilder.DropPrimaryKey(
                name: "PK_cb36eb8af8412bfa978f1165d78",
                table: "hashtag");

            migrationBuilder.DropPrimaryKey(
                name: "PK_8e90d7b6015f2c4518881b14753",
                table: "gallery_post");

            migrationBuilder.DropPrimaryKey(
                name: "PK_853ab02be39b8de45cd720cc15f",
                table: "gallery_like");

            migrationBuilder.DropPrimaryKey(
                name: "PK_c76c6e044bdf76ecf8bfb82a645",
                table: "following");

            migrationBuilder.DropPrimaryKey(
                name: "PK_53a9aa3725f7a3deb150b39dbfc",
                table: "follow_request");

            migrationBuilder.DropPrimaryKey(
                name: "PK_df74ce05e24999ee01ea0bc50a3",
                table: "emoji");

            migrationBuilder.DropPrimaryKey(
                name: "PK_7a0c089191f5ebdc214e0af808a",
                table: "drive_folder");

            migrationBuilder.DropPrimaryKey(
                name: "PK_43ddaaaf18c9e68029b7cbb032e",
                table: "drive_file");

            migrationBuilder.DropPrimaryKey(
                name: "PK_e94cda2f40a99b57e032a1a738b",
                table: "clip_note");

            migrationBuilder.DropPrimaryKey(
                name: "PK_f0685dac8d4dd056d7255670b75",
                table: "clip");

            migrationBuilder.DropPrimaryKey(
                name: "PK_44f7474496bcf2e4b741681146d",
                table: "channel_note_pin");

            migrationBuilder.DropPrimaryKey(
                name: "PK_8b104be7f7415113f2a02cd5bdd",
                table: "channel_following");

            migrationBuilder.DropPrimaryKey(
                name: "PK_590f33ee6ee7d76437acf362e39",
                table: "channel");

            migrationBuilder.DropPrimaryKey(
                name: "PK_e5d9a541cc1965ee7e048ea09dd",
                table: "blocking");

            migrationBuilder.DropPrimaryKey(
                name: "PK_19354ed146424a728c1112a8cbf",
                table: "auth_session");

            migrationBuilder.DropPrimaryKey(
                name: "PK_d0ba6786e093f1bcb497572a6b5",
                table: "attestation_challenge");

            migrationBuilder.DropPrimaryKey(
                name: "PK_9478629fc093d229df09e560aea",
                table: "app");

            migrationBuilder.DropPrimaryKey(
                name: "PK_c170b99775e1dccca947c9f2d5f",
                table: "antenna");

            migrationBuilder.DropPrimaryKey(
                name: "PK_4b90ad1f42681d97b2683890c5e",
                table: "announcement_read");

            migrationBuilder.DropPrimaryKey(
                name: "PK_e0ef0550174fd1099a308fd18a0",
                table: "announcement");

            migrationBuilder.DropPrimaryKey(
                name: "PK_f20f028607b2603deabd8182d12",
                table: "access_token");

            migrationBuilder.DropPrimaryKey(
                name: "PK_87873f5f5cc5c321a1306b2d18c",
                table: "abuse_user_report");

            migrationBuilder.AddPrimaryKey(
                name: "PK_webhook",
                table: "webhook",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_security_key",
                table: "user_security_key",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_publickey",
                table: "user_publickey",
                column: "userId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_profile",
                table: "user_profile",
                column: "userId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_pending",
                table: "user_pending",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_note_pin",
                table: "user_note_pin",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_list_member",
                table: "user_list_member",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_list",
                table: "user_list",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_keypair",
                table: "user_keypair",
                column: "userId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_group_member",
                table: "user_group_member",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_group_invite",
                table: "user_group_invite",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_group_invitation",
                table: "user_group_invitation",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_group",
                table: "user_group",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user",
                table: "user",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_used_username",
                table: "used_username",
                column: "username");

            migrationBuilder.AddPrimaryKey(
                name: "PK_sw_subscription",
                table: "sw_subscription",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_signin",
                table: "signin",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_session",
                table: "session",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_renote_muting",
                table: "renote_muting",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_relay",
                table: "relay",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_registry_item",
                table: "registry_item",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_registration_ticket",
                table: "registration_ticket",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_promo_read",
                table: "promo_read",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_promo_note",
                table: "promo_note",
                column: "noteId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_poll_vote",
                table: "poll_vote",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_poll",
                table: "poll",
                column: "noteId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_password_reset_request",
                table: "password_reset_request",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_page_like",
                table: "page_like",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_page",
                table: "page",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_oauth_token",
                table: "oauth_token",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_oauth_app",
                table: "oauth_app",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_notification",
                table: "notification",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_note_watching",
                table: "note_watching",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_note_unread",
                table: "note_unread",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_note_thread_muting",
                table: "note_thread_muting",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_note_reaction",
                table: "note_reaction",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_note_favorite",
                table: "note_favorite",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_note_edit",
                table: "note_edit",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_note",
                table: "note",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_muting",
                table: "muting",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_moderation_log",
                table: "moderation_log",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_meta",
                table: "meta",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_messaging_message",
                table: "messaging_message",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_instance",
                table: "instance",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_html_user_cache_entry",
                table: "html_user_cache_entry",
                column: "userId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_html_note_cache_entry",
                table: "html_note_cache_entry",
                column: "noteId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_hashtag",
                table: "hashtag",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_gallery_post",
                table: "gallery_post",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_gallery_like",
                table: "gallery_like",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_following",
                table: "following",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_follow_request",
                table: "follow_request",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_emoji",
                table: "emoji",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_drive_folder",
                table: "drive_folder",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_drive_file",
                table: "drive_file",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_clip_note",
                table: "clip_note",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_clip",
                table: "clip",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_channel_note_pin",
                table: "channel_note_pin",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_channel_following",
                table: "channel_following",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_channel",
                table: "channel",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_blocking",
                table: "blocking",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_auth_session",
                table: "auth_session",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_attestation_challenge",
                table: "attestation_challenge",
                columns: new[] { "id", "userId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_app",
                table: "app",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_antenna",
                table: "antenna",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_announcement_read",
                table: "announcement_read",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_announcement",
                table: "announcement",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_access_token",
                table: "access_token",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_abuse_user_report",
                table: "abuse_user_report",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_abuse_user_report_user_assigneeId",
                table: "abuse_user_report",
                column: "assigneeId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_abuse_user_report_user_reporterId",
                table: "abuse_user_report",
                column: "reporterId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_abuse_user_report_user_targetUserId",
                table: "abuse_user_report",
                column: "targetUserId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_access_token_app_appId",
                table: "access_token",
                column: "appId",
                principalTable: "app",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_access_token_user_userId",
                table: "access_token",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_announcement_read_announcement_announcementId",
                table: "announcement_read",
                column: "announcementId",
                principalTable: "announcement",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_announcement_read_user_userId",
                table: "announcement_read",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_antenna_user_group_member_UserGroupMemberId",
                table: "antenna",
                column: "UserGroupMemberId",
                principalTable: "user_group_member",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_antenna_user_list_userListId",
                table: "antenna",
                column: "userListId",
                principalTable: "user_list",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_antenna_user_userId",
                table: "antenna",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_app_user_userId",
                table: "app",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_attestation_challenge_user_userId",
                table: "attestation_challenge",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_auth_session_app_appId",
                table: "auth_session",
                column: "appId",
                principalTable: "app",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_auth_session_user_userId",
                table: "auth_session",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_blocking_user_blockeeId",
                table: "blocking",
                column: "blockeeId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_blocking_user_blockerId",
                table: "blocking",
                column: "blockerId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_channel_drive_file_bannerId",
                table: "channel",
                column: "bannerId",
                principalTable: "drive_file",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_channel_user_userId",
                table: "channel",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_channel_following_channel_followeeId",
                table: "channel_following",
                column: "followeeId",
                principalTable: "channel",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_channel_following_user_followerId",
                table: "channel_following",
                column: "followerId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_channel_note_pin_channel_channelId",
                table: "channel_note_pin",
                column: "channelId",
                principalTable: "channel",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_channel_note_pin_note_noteId",
                table: "channel_note_pin",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_clip_user_userId",
                table: "clip",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_clip_note_clip_clipId",
                table: "clip_note",
                column: "clipId",
                principalTable: "clip",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_clip_note_note_noteId",
                table: "clip_note",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_drive_file_drive_folder_folderId",
                table: "drive_file",
                column: "folderId",
                principalTable: "drive_folder",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_drive_file_user_userId",
                table: "drive_file",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_drive_folder_drive_folder_parentId",
                table: "drive_folder",
                column: "parentId",
                principalTable: "drive_folder",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_drive_folder_user_userId",
                table: "drive_folder",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_follow_request_user_followeeId",
                table: "follow_request",
                column: "followeeId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_follow_request_user_followerId",
                table: "follow_request",
                column: "followerId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_following_user_followeeId",
                table: "following",
                column: "followeeId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_following_user_followerId",
                table: "following",
                column: "followerId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_gallery_like_gallery_post_postId",
                table: "gallery_like",
                column: "postId",
                principalTable: "gallery_post",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_gallery_like_user_userId",
                table: "gallery_like",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_gallery_post_user_userId",
                table: "gallery_post",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_html_note_cache_entry_note_noteId",
                table: "html_note_cache_entry",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_html_user_cache_entry_user_userId",
                table: "html_user_cache_entry",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_messaging_message_drive_file_fileId",
                table: "messaging_message",
                column: "fileId",
                principalTable: "drive_file",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_messaging_message_user_group_groupId",
                table: "messaging_message",
                column: "groupId",
                principalTable: "user_group",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_messaging_message_user_recipientId",
                table: "messaging_message",
                column: "recipientId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_messaging_message_user_userId",
                table: "messaging_message",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_moderation_log_user_userId",
                table: "moderation_log",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_muting_user_muteeId",
                table: "muting",
                column: "muteeId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_muting_user_muterId",
                table: "muting",
                column: "muterId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_note_channel_channelId",
                table: "note",
                column: "channelId",
                principalTable: "channel",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_note_note_renoteId",
                table: "note",
                column: "renoteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_note_note_replyId",
                table: "note",
                column: "replyId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_note_user_userId",
                table: "note",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_note_edit_note_noteId",
                table: "note_edit",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_note_favorite_note_noteId",
                table: "note_favorite",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_note_favorite_user_userId",
                table: "note_favorite",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_note_reaction_note_noteId",
                table: "note_reaction",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_note_reaction_user_userId",
                table: "note_reaction",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_note_thread_muting_user_userId",
                table: "note_thread_muting",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_note_unread_note_noteId",
                table: "note_unread",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_note_unread_user_userId",
                table: "note_unread",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_note_watching_note_noteId",
                table: "note_watching",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_note_watching_user_userId",
                table: "note_watching",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_notification_access_token_appAccessTokenId",
                table: "notification",
                column: "appAccessTokenId",
                principalTable: "access_token",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_notification_follow_request_followRequestId",
                table: "notification",
                column: "followRequestId",
                principalTable: "follow_request",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_notification_note_noteId",
                table: "notification",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_notification_user_group_invitation_userGroupInvitationId",
                table: "notification",
                column: "userGroupInvitationId",
                principalTable: "user_group_invitation",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_notification_user_notifieeId",
                table: "notification",
                column: "notifieeId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_notification_user_notifierId",
                table: "notification",
                column: "notifierId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_oauth_token_oauth_app_appId",
                table: "oauth_token",
                column: "appId",
                principalTable: "oauth_app",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_oauth_token_user_userId",
                table: "oauth_token",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_page_drive_file_eyeCatchingImageId",
                table: "page",
                column: "eyeCatchingImageId",
                principalTable: "drive_file",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_page_user_userId",
                table: "page",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_page_like_page_pageId",
                table: "page_like",
                column: "pageId",
                principalTable: "page",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_page_like_user_userId",
                table: "page_like",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_password_reset_request_user_userId",
                table: "password_reset_request",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_poll_note_noteId",
                table: "poll",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_poll_vote_note_noteId",
                table: "poll_vote",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_poll_vote_user_userId",
                table: "poll_vote",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_promo_note_note_noteId",
                table: "promo_note",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_promo_read_note_noteId",
                table: "promo_read",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_promo_read_user_userId",
                table: "promo_read",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_registry_item_user_userId",
                table: "registry_item",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_renote_muting_user_muteeId",
                table: "renote_muting",
                column: "muteeId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_renote_muting_user_muterId",
                table: "renote_muting",
                column: "muterId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_session_user_userId",
                table: "session",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_signin_user_userId",
                table: "signin",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_sw_subscription_user_userId",
                table: "sw_subscription",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_drive_file_avatarId",
                table: "user",
                column: "avatarId",
                principalTable: "drive_file",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_user_drive_file_bannerId",
                table: "user",
                column: "bannerId",
                principalTable: "drive_file",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_user_group_user_userId",
                table: "user_group",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_group_invitation_user_group_userGroupId",
                table: "user_group_invitation",
                column: "userGroupId",
                principalTable: "user_group",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_group_invitation_user_userId",
                table: "user_group_invitation",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_group_invite_user_group_userGroupId",
                table: "user_group_invite",
                column: "userGroupId",
                principalTable: "user_group",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_group_invite_user_userId",
                table: "user_group_invite",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_group_member_user_group_userGroupId",
                table: "user_group_member",
                column: "userGroupId",
                principalTable: "user_group",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_group_member_user_userId",
                table: "user_group_member",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_keypair_user_userId",
                table: "user_keypair",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_list_user_userId",
                table: "user_list",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_list_member_user_list_userListId",
                table: "user_list_member",
                column: "userListId",
                principalTable: "user_list",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_list_member_user_userId",
                table: "user_list_member",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_note_pin_note_noteId",
                table: "user_note_pin",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_note_pin_user_userId",
                table: "user_note_pin",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_profile_page_pinnedPageId",
                table: "user_profile",
                column: "pinnedPageId",
                principalTable: "page",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_user_profile_user_userId",
                table: "user_profile",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_publickey_user_userId",
                table: "user_publickey",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_security_key_user_userId",
                table: "user_security_key",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_webhook_user_userId",
                table: "webhook",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_abuse_user_report_user_assigneeId",
                table: "abuse_user_report");

            migrationBuilder.DropForeignKey(
                name: "FK_abuse_user_report_user_reporterId",
                table: "abuse_user_report");

            migrationBuilder.DropForeignKey(
                name: "FK_abuse_user_report_user_targetUserId",
                table: "abuse_user_report");

            migrationBuilder.DropForeignKey(
                name: "FK_access_token_app_appId",
                table: "access_token");

            migrationBuilder.DropForeignKey(
                name: "FK_access_token_user_userId",
                table: "access_token");

            migrationBuilder.DropForeignKey(
                name: "FK_announcement_read_announcement_announcementId",
                table: "announcement_read");

            migrationBuilder.DropForeignKey(
                name: "FK_announcement_read_user_userId",
                table: "announcement_read");

            migrationBuilder.DropForeignKey(
                name: "FK_antenna_user_group_member_UserGroupMemberId",
                table: "antenna");

            migrationBuilder.DropForeignKey(
                name: "FK_antenna_user_list_userListId",
                table: "antenna");

            migrationBuilder.DropForeignKey(
                name: "FK_antenna_user_userId",
                table: "antenna");

            migrationBuilder.DropForeignKey(
                name: "FK_app_user_userId",
                table: "app");

            migrationBuilder.DropForeignKey(
                name: "FK_attestation_challenge_user_userId",
                table: "attestation_challenge");

            migrationBuilder.DropForeignKey(
                name: "FK_auth_session_app_appId",
                table: "auth_session");

            migrationBuilder.DropForeignKey(
                name: "FK_auth_session_user_userId",
                table: "auth_session");

            migrationBuilder.DropForeignKey(
                name: "FK_blocking_user_blockeeId",
                table: "blocking");

            migrationBuilder.DropForeignKey(
                name: "FK_blocking_user_blockerId",
                table: "blocking");

            migrationBuilder.DropForeignKey(
                name: "FK_channel_drive_file_bannerId",
                table: "channel");

            migrationBuilder.DropForeignKey(
                name: "FK_channel_user_userId",
                table: "channel");

            migrationBuilder.DropForeignKey(
                name: "FK_channel_following_channel_followeeId",
                table: "channel_following");

            migrationBuilder.DropForeignKey(
                name: "FK_channel_following_user_followerId",
                table: "channel_following");

            migrationBuilder.DropForeignKey(
                name: "FK_channel_note_pin_channel_channelId",
                table: "channel_note_pin");

            migrationBuilder.DropForeignKey(
                name: "FK_channel_note_pin_note_noteId",
                table: "channel_note_pin");

            migrationBuilder.DropForeignKey(
                name: "FK_clip_user_userId",
                table: "clip");

            migrationBuilder.DropForeignKey(
                name: "FK_clip_note_clip_clipId",
                table: "clip_note");

            migrationBuilder.DropForeignKey(
                name: "FK_clip_note_note_noteId",
                table: "clip_note");

            migrationBuilder.DropForeignKey(
                name: "FK_drive_file_drive_folder_folderId",
                table: "drive_file");

            migrationBuilder.DropForeignKey(
                name: "FK_drive_file_user_userId",
                table: "drive_file");

            migrationBuilder.DropForeignKey(
                name: "FK_drive_folder_drive_folder_parentId",
                table: "drive_folder");

            migrationBuilder.DropForeignKey(
                name: "FK_drive_folder_user_userId",
                table: "drive_folder");

            migrationBuilder.DropForeignKey(
                name: "FK_follow_request_user_followeeId",
                table: "follow_request");

            migrationBuilder.DropForeignKey(
                name: "FK_follow_request_user_followerId",
                table: "follow_request");

            migrationBuilder.DropForeignKey(
                name: "FK_following_user_followeeId",
                table: "following");

            migrationBuilder.DropForeignKey(
                name: "FK_following_user_followerId",
                table: "following");

            migrationBuilder.DropForeignKey(
                name: "FK_gallery_like_gallery_post_postId",
                table: "gallery_like");

            migrationBuilder.DropForeignKey(
                name: "FK_gallery_like_user_userId",
                table: "gallery_like");

            migrationBuilder.DropForeignKey(
                name: "FK_gallery_post_user_userId",
                table: "gallery_post");

            migrationBuilder.DropForeignKey(
                name: "FK_html_note_cache_entry_note_noteId",
                table: "html_note_cache_entry");

            migrationBuilder.DropForeignKey(
                name: "FK_html_user_cache_entry_user_userId",
                table: "html_user_cache_entry");

            migrationBuilder.DropForeignKey(
                name: "FK_messaging_message_drive_file_fileId",
                table: "messaging_message");

            migrationBuilder.DropForeignKey(
                name: "FK_messaging_message_user_group_groupId",
                table: "messaging_message");

            migrationBuilder.DropForeignKey(
                name: "FK_messaging_message_user_recipientId",
                table: "messaging_message");

            migrationBuilder.DropForeignKey(
                name: "FK_messaging_message_user_userId",
                table: "messaging_message");

            migrationBuilder.DropForeignKey(
                name: "FK_moderation_log_user_userId",
                table: "moderation_log");

            migrationBuilder.DropForeignKey(
                name: "FK_muting_user_muteeId",
                table: "muting");

            migrationBuilder.DropForeignKey(
                name: "FK_muting_user_muterId",
                table: "muting");

            migrationBuilder.DropForeignKey(
                name: "FK_note_channel_channelId",
                table: "note");

            migrationBuilder.DropForeignKey(
                name: "FK_note_note_renoteId",
                table: "note");

            migrationBuilder.DropForeignKey(
                name: "FK_note_note_replyId",
                table: "note");

            migrationBuilder.DropForeignKey(
                name: "FK_note_user_userId",
                table: "note");

            migrationBuilder.DropForeignKey(
                name: "FK_note_edit_note_noteId",
                table: "note_edit");

            migrationBuilder.DropForeignKey(
                name: "FK_note_favorite_note_noteId",
                table: "note_favorite");

            migrationBuilder.DropForeignKey(
                name: "FK_note_favorite_user_userId",
                table: "note_favorite");

            migrationBuilder.DropForeignKey(
                name: "FK_note_reaction_note_noteId",
                table: "note_reaction");

            migrationBuilder.DropForeignKey(
                name: "FK_note_reaction_user_userId",
                table: "note_reaction");

            migrationBuilder.DropForeignKey(
                name: "FK_note_thread_muting_user_userId",
                table: "note_thread_muting");

            migrationBuilder.DropForeignKey(
                name: "FK_note_unread_note_noteId",
                table: "note_unread");

            migrationBuilder.DropForeignKey(
                name: "FK_note_unread_user_userId",
                table: "note_unread");

            migrationBuilder.DropForeignKey(
                name: "FK_note_watching_note_noteId",
                table: "note_watching");

            migrationBuilder.DropForeignKey(
                name: "FK_note_watching_user_userId",
                table: "note_watching");

            migrationBuilder.DropForeignKey(
                name: "FK_notification_access_token_appAccessTokenId",
                table: "notification");

            migrationBuilder.DropForeignKey(
                name: "FK_notification_follow_request_followRequestId",
                table: "notification");

            migrationBuilder.DropForeignKey(
                name: "FK_notification_note_noteId",
                table: "notification");

            migrationBuilder.DropForeignKey(
                name: "FK_notification_user_group_invitation_userGroupInvitationId",
                table: "notification");

            migrationBuilder.DropForeignKey(
                name: "FK_notification_user_notifieeId",
                table: "notification");

            migrationBuilder.DropForeignKey(
                name: "FK_notification_user_notifierId",
                table: "notification");

            migrationBuilder.DropForeignKey(
                name: "FK_oauth_token_oauth_app_appId",
                table: "oauth_token");

            migrationBuilder.DropForeignKey(
                name: "FK_oauth_token_user_userId",
                table: "oauth_token");

            migrationBuilder.DropForeignKey(
                name: "FK_page_drive_file_eyeCatchingImageId",
                table: "page");

            migrationBuilder.DropForeignKey(
                name: "FK_page_user_userId",
                table: "page");

            migrationBuilder.DropForeignKey(
                name: "FK_page_like_page_pageId",
                table: "page_like");

            migrationBuilder.DropForeignKey(
                name: "FK_page_like_user_userId",
                table: "page_like");

            migrationBuilder.DropForeignKey(
                name: "FK_password_reset_request_user_userId",
                table: "password_reset_request");

            migrationBuilder.DropForeignKey(
                name: "FK_poll_note_noteId",
                table: "poll");

            migrationBuilder.DropForeignKey(
                name: "FK_poll_vote_note_noteId",
                table: "poll_vote");

            migrationBuilder.DropForeignKey(
                name: "FK_poll_vote_user_userId",
                table: "poll_vote");

            migrationBuilder.DropForeignKey(
                name: "FK_promo_note_note_noteId",
                table: "promo_note");

            migrationBuilder.DropForeignKey(
                name: "FK_promo_read_note_noteId",
                table: "promo_read");

            migrationBuilder.DropForeignKey(
                name: "FK_promo_read_user_userId",
                table: "promo_read");

            migrationBuilder.DropForeignKey(
                name: "FK_registry_item_user_userId",
                table: "registry_item");

            migrationBuilder.DropForeignKey(
                name: "FK_renote_muting_user_muteeId",
                table: "renote_muting");

            migrationBuilder.DropForeignKey(
                name: "FK_renote_muting_user_muterId",
                table: "renote_muting");

            migrationBuilder.DropForeignKey(
                name: "FK_session_user_userId",
                table: "session");

            migrationBuilder.DropForeignKey(
                name: "FK_signin_user_userId",
                table: "signin");

            migrationBuilder.DropForeignKey(
                name: "FK_sw_subscription_user_userId",
                table: "sw_subscription");

            migrationBuilder.DropForeignKey(
                name: "FK_user_drive_file_avatarId",
                table: "user");

            migrationBuilder.DropForeignKey(
                name: "FK_user_drive_file_bannerId",
                table: "user");

            migrationBuilder.DropForeignKey(
                name: "FK_user_group_user_userId",
                table: "user_group");

            migrationBuilder.DropForeignKey(
                name: "FK_user_group_invitation_user_group_userGroupId",
                table: "user_group_invitation");

            migrationBuilder.DropForeignKey(
                name: "FK_user_group_invitation_user_userId",
                table: "user_group_invitation");

            migrationBuilder.DropForeignKey(
                name: "FK_user_group_invite_user_group_userGroupId",
                table: "user_group_invite");

            migrationBuilder.DropForeignKey(
                name: "FK_user_group_invite_user_userId",
                table: "user_group_invite");

            migrationBuilder.DropForeignKey(
                name: "FK_user_group_member_user_group_userGroupId",
                table: "user_group_member");

            migrationBuilder.DropForeignKey(
                name: "FK_user_group_member_user_userId",
                table: "user_group_member");

            migrationBuilder.DropForeignKey(
                name: "FK_user_keypair_user_userId",
                table: "user_keypair");

            migrationBuilder.DropForeignKey(
                name: "FK_user_list_user_userId",
                table: "user_list");

            migrationBuilder.DropForeignKey(
                name: "FK_user_list_member_user_list_userListId",
                table: "user_list_member");

            migrationBuilder.DropForeignKey(
                name: "FK_user_list_member_user_userId",
                table: "user_list_member");

            migrationBuilder.DropForeignKey(
                name: "FK_user_note_pin_note_noteId",
                table: "user_note_pin");

            migrationBuilder.DropForeignKey(
                name: "FK_user_note_pin_user_userId",
                table: "user_note_pin");

            migrationBuilder.DropForeignKey(
                name: "FK_user_profile_page_pinnedPageId",
                table: "user_profile");

            migrationBuilder.DropForeignKey(
                name: "FK_user_profile_user_userId",
                table: "user_profile");

            migrationBuilder.DropForeignKey(
                name: "FK_user_publickey_user_userId",
                table: "user_publickey");

            migrationBuilder.DropForeignKey(
                name: "FK_user_security_key_user_userId",
                table: "user_security_key");

            migrationBuilder.DropForeignKey(
                name: "FK_webhook_user_userId",
                table: "webhook");

            migrationBuilder.DropPrimaryKey(
                name: "PK_webhook",
                table: "webhook");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_security_key",
                table: "user_security_key");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_publickey",
                table: "user_publickey");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_profile",
                table: "user_profile");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_pending",
                table: "user_pending");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_note_pin",
                table: "user_note_pin");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_list_member",
                table: "user_list_member");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_list",
                table: "user_list");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_keypair",
                table: "user_keypair");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_group_member",
                table: "user_group_member");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_group_invite",
                table: "user_group_invite");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_group_invitation",
                table: "user_group_invitation");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_group",
                table: "user_group");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user",
                table: "user");

            migrationBuilder.DropPrimaryKey(
                name: "PK_used_username",
                table: "used_username");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sw_subscription",
                table: "sw_subscription");

            migrationBuilder.DropPrimaryKey(
                name: "PK_signin",
                table: "signin");

            migrationBuilder.DropPrimaryKey(
                name: "PK_session",
                table: "session");

            migrationBuilder.DropPrimaryKey(
                name: "PK_renote_muting",
                table: "renote_muting");

            migrationBuilder.DropPrimaryKey(
                name: "PK_relay",
                table: "relay");

            migrationBuilder.DropPrimaryKey(
                name: "PK_registry_item",
                table: "registry_item");

            migrationBuilder.DropPrimaryKey(
                name: "PK_registration_ticket",
                table: "registration_ticket");

            migrationBuilder.DropPrimaryKey(
                name: "PK_promo_read",
                table: "promo_read");

            migrationBuilder.DropPrimaryKey(
                name: "PK_promo_note",
                table: "promo_note");

            migrationBuilder.DropPrimaryKey(
                name: "PK_poll_vote",
                table: "poll_vote");

            migrationBuilder.DropPrimaryKey(
                name: "PK_poll",
                table: "poll");

            migrationBuilder.DropPrimaryKey(
                name: "PK_password_reset_request",
                table: "password_reset_request");

            migrationBuilder.DropPrimaryKey(
                name: "PK_page_like",
                table: "page_like");

            migrationBuilder.DropPrimaryKey(
                name: "PK_page",
                table: "page");

            migrationBuilder.DropPrimaryKey(
                name: "PK_oauth_token",
                table: "oauth_token");

            migrationBuilder.DropPrimaryKey(
                name: "PK_oauth_app",
                table: "oauth_app");

            migrationBuilder.DropPrimaryKey(
                name: "PK_notification",
                table: "notification");

            migrationBuilder.DropPrimaryKey(
                name: "PK_note_watching",
                table: "note_watching");

            migrationBuilder.DropPrimaryKey(
                name: "PK_note_unread",
                table: "note_unread");

            migrationBuilder.DropPrimaryKey(
                name: "PK_note_thread_muting",
                table: "note_thread_muting");

            migrationBuilder.DropPrimaryKey(
                name: "PK_note_reaction",
                table: "note_reaction");

            migrationBuilder.DropPrimaryKey(
                name: "PK_note_favorite",
                table: "note_favorite");

            migrationBuilder.DropPrimaryKey(
                name: "PK_note_edit",
                table: "note_edit");

            migrationBuilder.DropPrimaryKey(
                name: "PK_note",
                table: "note");

            migrationBuilder.DropPrimaryKey(
                name: "PK_muting",
                table: "muting");

            migrationBuilder.DropPrimaryKey(
                name: "PK_moderation_log",
                table: "moderation_log");

            migrationBuilder.DropPrimaryKey(
                name: "PK_meta",
                table: "meta");

            migrationBuilder.DropPrimaryKey(
                name: "PK_messaging_message",
                table: "messaging_message");

            migrationBuilder.DropPrimaryKey(
                name: "PK_instance",
                table: "instance");

            migrationBuilder.DropPrimaryKey(
                name: "PK_html_user_cache_entry",
                table: "html_user_cache_entry");

            migrationBuilder.DropPrimaryKey(
                name: "PK_html_note_cache_entry",
                table: "html_note_cache_entry");

            migrationBuilder.DropPrimaryKey(
                name: "PK_hashtag",
                table: "hashtag");

            migrationBuilder.DropPrimaryKey(
                name: "PK_gallery_post",
                table: "gallery_post");

            migrationBuilder.DropPrimaryKey(
                name: "PK_gallery_like",
                table: "gallery_like");

            migrationBuilder.DropPrimaryKey(
                name: "PK_following",
                table: "following");

            migrationBuilder.DropPrimaryKey(
                name: "PK_follow_request",
                table: "follow_request");

            migrationBuilder.DropPrimaryKey(
                name: "PK_emoji",
                table: "emoji");

            migrationBuilder.DropPrimaryKey(
                name: "PK_drive_folder",
                table: "drive_folder");

            migrationBuilder.DropPrimaryKey(
                name: "PK_drive_file",
                table: "drive_file");

            migrationBuilder.DropPrimaryKey(
                name: "PK_clip_note",
                table: "clip_note");

            migrationBuilder.DropPrimaryKey(
                name: "PK_clip",
                table: "clip");

            migrationBuilder.DropPrimaryKey(
                name: "PK_channel_note_pin",
                table: "channel_note_pin");

            migrationBuilder.DropPrimaryKey(
                name: "PK_channel_following",
                table: "channel_following");

            migrationBuilder.DropPrimaryKey(
                name: "PK_channel",
                table: "channel");

            migrationBuilder.DropPrimaryKey(
                name: "PK_blocking",
                table: "blocking");

            migrationBuilder.DropPrimaryKey(
                name: "PK_auth_session",
                table: "auth_session");

            migrationBuilder.DropPrimaryKey(
                name: "PK_attestation_challenge",
                table: "attestation_challenge");

            migrationBuilder.DropPrimaryKey(
                name: "PK_app",
                table: "app");

            migrationBuilder.DropPrimaryKey(
                name: "PK_antenna",
                table: "antenna");

            migrationBuilder.DropPrimaryKey(
                name: "PK_announcement_read",
                table: "announcement_read");

            migrationBuilder.DropPrimaryKey(
                name: "PK_announcement",
                table: "announcement");

            migrationBuilder.DropPrimaryKey(
                name: "PK_access_token",
                table: "access_token");

            migrationBuilder.DropPrimaryKey(
                name: "PK_abuse_user_report",
                table: "abuse_user_report");

            migrationBuilder.AddPrimaryKey(
                name: "PK_e6765510c2d078db49632b59020",
                table: "webhook",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_3e508571121ab39c5f85d10c166",
                table: "user_security_key",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_10c146e4b39b443ede016f6736d",
                table: "user_publickey",
                column: "userId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_51cb79b5555effaf7d69ba1cff9",
                table: "user_profile",
                column: "userId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_d4c84e013c98ec02d19b8fbbafa",
                table: "user_pending",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_a6a2dad4ae000abce2ea9d9b103",
                table: "user_note_pin",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_11abb3768da1c5f8de101c9df45",
                table: "user_list_member",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_87bab75775fd9b1ff822b656402",
                table: "user_list",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_f4853eb41ab722fe05f81cedeb6",
                table: "user_keypair",
                column: "userId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_15f2425885253c5507e1599cfe7",
                table: "user_group_member",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_3893884af0d3a5f4d01e7921a97",
                table: "user_group_invite",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_160c63ec02bf23f6a5c5e8140d6",
                table: "user_group_invitation",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_3c29fba6fe013ec8724378ce7c9",
                table: "user_group",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_cace4a159ff9f2512dd42373760",
                table: "user",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_78fd79d2d24c6ac2f4cc9a31a5d",
                table: "used_username",
                column: "username");

            migrationBuilder.AddPrimaryKey(
                name: "PK_e8f763631530051b95eb6279b91",
                table: "sw_subscription",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_9e96ddc025712616fc492b3b588",
                table: "signin",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_f55da76ac1c3ac420f444d2ff11",
                table: "session",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_renoteMuting_id",
                table: "renote_muting",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_78ebc9cfddf4292633b7ba57aee",
                table: "relay",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_64b3f7e6008b4d89b826cd3af95",
                table: "registry_item",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_f11696b6fafcf3662d4292734f8",
                table: "registration_ticket",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_61917c1541002422b703318b7c9",
                table: "promo_read",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_e263909ca4fe5d57f8d4230dd5c",
                table: "promo_note",
                column: "noteId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_fd002d371201c472490ba89c6a0",
                table: "poll_vote",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_da851e06d0dfe2ef397d8b1bf1b",
                table: "poll",
                column: "noteId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_fcf4b02eae1403a2edaf87fd074",
                table: "password_reset_request",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_813f034843af992d3ae0f43c64c",
                table: "page_like",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_742f4117e065c5b6ad21b37ba1f",
                table: "page",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_7e6a25a3cc4395d1658f5b89c73",
                table: "oauth_token",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_3256b97c0a3ee2d67240805dca4",
                table: "oauth_app",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_705b6c7cdf9b2c2ff7ac7872cb7",
                table: "notification",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_49286fdb23725945a74aa27d757",
                table: "note_watching",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_1904eda61a784f57e6e51fa9c1f",
                table: "note_unread",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ec5936d94d1a0369646d12a3a47",
                table: "note_thread_muting",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_767ec729b108799b587a3fcc9cf",
                table: "note_reaction",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_af0da35a60b9fa4463a62082b36",
                table: "note_favorite",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_736fc6e0d4e222ecc6f82058e08",
                table: "note_edit",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_96d0c172a4fba276b1bbed43058",
                table: "note",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_2e92d06c8b5c602eeb27ca9ba48",
                table: "muting",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_d0adca6ecfd068db83e4526cc26",
                table: "moderation_log",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_c4c17a6c2bd7651338b60fc590b",
                table: "meta",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_db398fd79dc95d0eb8c30456eaa",
                table: "messaging_message",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_eaf60e4a0c399c9935413e06474",
                table: "instance",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_920b9474e3c9cae3f3c37c057e1",
                table: "html_user_cache_entry",
                column: "userId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_6ef86ec901b2017cbe82d3a8286",
                table: "html_note_cache_entry",
                column: "noteId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_cb36eb8af8412bfa978f1165d78",
                table: "hashtag",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_8e90d7b6015f2c4518881b14753",
                table: "gallery_post",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_853ab02be39b8de45cd720cc15f",
                table: "gallery_like",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_c76c6e044bdf76ecf8bfb82a645",
                table: "following",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_53a9aa3725f7a3deb150b39dbfc",
                table: "follow_request",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_df74ce05e24999ee01ea0bc50a3",
                table: "emoji",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_7a0c089191f5ebdc214e0af808a",
                table: "drive_folder",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_43ddaaaf18c9e68029b7cbb032e",
                table: "drive_file",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_e94cda2f40a99b57e032a1a738b",
                table: "clip_note",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_f0685dac8d4dd056d7255670b75",
                table: "clip",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_44f7474496bcf2e4b741681146d",
                table: "channel_note_pin",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_8b104be7f7415113f2a02cd5bdd",
                table: "channel_following",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_590f33ee6ee7d76437acf362e39",
                table: "channel",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_e5d9a541cc1965ee7e048ea09dd",
                table: "blocking",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_19354ed146424a728c1112a8cbf",
                table: "auth_session",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_d0ba6786e093f1bcb497572a6b5",
                table: "attestation_challenge",
                columns: new[] { "id", "userId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_9478629fc093d229df09e560aea",
                table: "app",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_c170b99775e1dccca947c9f2d5f",
                table: "antenna",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_4b90ad1f42681d97b2683890c5e",
                table: "announcement_read",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_e0ef0550174fd1099a308fd18a0",
                table: "announcement",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_f20f028607b2603deabd8182d12",
                table: "access_token",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_87873f5f5cc5c321a1306b2d18c",
                table: "abuse_user_report",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_04cc96756f89d0b7f9473e8cdf3",
                table: "abuse_user_report",
                column: "reporterId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_08b883dd5fdd6f9c4c1572b36de",
                table: "abuse_user_report",
                column: "assigneeId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_a9021cc2e1feb5f72d3db6e9f5f",
                table: "abuse_user_report",
                column: "targetUserId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_9949557d0e1b2c19e5344c171e9",
                table: "access_token",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_a3ff16c90cc87a82a0b5959e560",
                table: "access_token",
                column: "appId",
                principalTable: "app",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_603a7b1e7aa0533c6c88e9bfafe",
                table: "announcement_read",
                column: "announcementId",
                principalTable: "announcement",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_8288151386172b8109f7239ab28",
                table: "announcement_read",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_6446c571a0e8d0f05f01c789096",
                table: "antenna",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_709d7d32053d0dd7620f678eeb9",
                table: "antenna",
                column: "userListId",
                principalTable: "user_list",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ccbf5a8c0be4511133dcc50ddeb",
                table: "antenna",
                column: "UserGroupMemberId",
                principalTable: "user_group_member",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_3f5b0899ef90527a3462d7c2cb3",
                table: "app",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_f1a461a618fa1755692d0e0d592",
                table: "attestation_challenge",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_c072b729d71697f959bde66ade0",
                table: "auth_session",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_dbe037d4bddd17b03a1dc778dee",
                table: "auth_session",
                column: "appId",
                principalTable: "app",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_0627125f1a8a42c9a1929edb552",
                table: "blocking",
                column: "blockerId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_2cd4a2743a99671308f5417759e",
                table: "blocking",
                column: "blockeeId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_823bae55bd81b3be6e05cff4383",
                table: "channel",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_999da2bcc7efadbfe0e92d3bc19",
                table: "channel",
                column: "bannerId",
                principalTable: "drive_file",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_0e43068c3f92cab197c3d3cd86e",
                table: "channel_following",
                column: "followeeId",
                principalTable: "channel",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_6d8084ec9496e7334a4602707e1",
                table: "channel_following",
                column: "followerId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_10b19ef67d297ea9de325cd4502",
                table: "channel_note_pin",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_8125f950afd3093acb10d2db8a8",
                table: "channel_note_pin",
                column: "channelId",
                principalTable: "channel",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_2b5ec6c574d6802c94c80313fb2",
                table: "clip",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_a012eaf5c87c65da1deb5fdbfa3",
                table: "clip_note",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ebe99317bbbe9968a0c6f579adf",
                table: "clip_note",
                column: "clipId",
                principalTable: "clip",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_860fa6f6c7df5bb887249fba22e",
                table: "drive_file",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_bb90d1956dafc4068c28aa7560a",
                table: "drive_file",
                column: "folderId",
                principalTable: "drive_folder",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_00ceffb0cdc238b3233294f08f2",
                table: "drive_folder",
                column: "parentId",
                principalTable: "drive_folder",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_f4fc06e49c0171c85f1c48060d2",
                table: "drive_folder",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_12c01c0d1a79f77d9f6c15fadd2",
                table: "follow_request",
                column: "followeeId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_a7fd92dd6dc519e6fb435dd108f",
                table: "follow_request",
                column: "followerId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_24e0042143a18157b234df186c3",
                table: "following",
                column: "followeeId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_6516c5a6f3c015b4eed39978be5",
                table: "following",
                column: "followerId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_8fd5215095473061855ceb948cf",
                table: "gallery_like",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_b1cb568bfe569e47b7051699fc8",
                table: "gallery_like",
                column: "postId",
                principalTable: "gallery_post",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_985b836dddd8615e432d7043ddb",
                table: "gallery_post",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_6ef86ec901b2017cbe82d3a8286",
                table: "html_note_cache_entry",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_920b9474e3c9cae3f3c37c057e1",
                table: "html_user_cache_entry",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_2c4be03b446884f9e9c502135be",
                table: "messaging_message",
                column: "groupId",
                principalTable: "user_group",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_535def119223ac05ad3fa9ef64b",
                table: "messaging_message",
                column: "fileId",
                principalTable: "drive_file",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_5377c307783fce2b6d352e1203b",
                table: "messaging_message",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_cac14a4e3944454a5ce7daa5142",
                table: "messaging_message",
                column: "recipientId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_a08ad074601d204e0f69da9a954",
                table: "moderation_log",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_93060675b4a79a577f31d260c67",
                table: "muting",
                column: "muterId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ec96b4fed9dae517e0dbbe0675c",
                table: "muting",
                column: "muteeId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_17cb3553c700a4985dff5a30ff5",
                table: "note",
                column: "replyId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_52ccc804d7c69037d558bac4c96",
                table: "note",
                column: "renoteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_5b87d9d19127bd5d92026017a7b",
                table: "note",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_f22169eb10657bded6d875ac8f9",
                table: "note",
                column: "channelId",
                principalTable: "channel",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_702ad5ae993a672e4fbffbcd38c",
                table: "note_edit",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_0e00498f180193423c992bc4370",
                table: "note_favorite",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_47f4b1892f5d6ba8efb3057d81a",
                table: "note_favorite",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_13761f64257f40c5636d0ff95ee",
                table: "note_reaction",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_45145e4953780f3cd5656f0ea6a",
                table: "note_reaction",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_29c11c7deb06615076f8c95b80a",
                table: "note_thread_muting",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_56b0166d34ddae49d8ef7610bb9",
                table: "note_unread",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_e637cba4dc4410218c4251260e4",
                table: "note_unread",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_03e7028ab8388a3f5e3ce2a8619",
                table: "note_watching",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_b0134ec406e8d09a540f8182888",
                table: "note_watching",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_3b4e96eec8d36a8bbb9d02aa710",
                table: "notification",
                column: "notifierId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_3c601b70a1066d2c8b517094cb9",
                table: "notification",
                column: "notifieeId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_769cb6b73a1efe22ddf733ac453",
                table: "notification",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_8fe87814e978053a53b1beb7e98",
                table: "notification",
                column: "userGroupInvitationId",
                principalTable: "user_group_invitation",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_bd7fab507621e635b32cd31892c",
                table: "notification",
                column: "followRequestId",
                principalTable: "follow_request",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_e22bf6bda77b6adc1fd9e75c8c9",
                table: "notification",
                column: "appAccessTokenId",
                principalTable: "access_token",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_6d3ef28ea647b1449ba79690874",
                table: "oauth_token",
                column: "appId",
                principalTable: "oauth_app",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_f6b4b1ac66b753feab5d831ba04",
                table: "oauth_token",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_a9ca79ad939bf06066b81c9d3aa",
                table: "page",
                column: "eyeCatchingImageId",
                principalTable: "drive_file",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ae1d917992dd0c9d9bbdad06c4a",
                table: "page",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_0e61efab7f88dbb79c9166dbb48",
                table: "page_like",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_cf8782626dced3176038176a847",
                table: "page_like",
                column: "pageId",
                principalTable: "page",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_4bb7fd4a34492ae0e6cc8d30ac8",
                table: "password_reset_request",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_da851e06d0dfe2ef397d8b1bf1b",
                table: "poll",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_66d2bd2ee31d14bcc23069a89f8",
                table: "poll_vote",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_aecfbd5ef60374918e63ee95fa7",
                table: "poll_vote",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_e263909ca4fe5d57f8d4230dd5c",
                table: "promo_note",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_9657d55550c3d37bfafaf7d4b05",
                table: "promo_read",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_a46a1a603ecee695d7db26da5f4",
                table: "promo_read",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_fb9d21ba0abb83223263df6bcb3",
                table: "registry_item",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_7aa72a5fe76019bfe8e5e0e8b7d",
                table: "renote_muting",
                column: "muterId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_7eac97594bcac5ffcf2068089b6",
                table: "renote_muting",
                column: "muteeId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_3d2f174ef04fb312fdebd0ddc53",
                table: "session",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_2c308dbdc50d94dc625670055f7",
                table: "signin",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_97754ca6f2baff9b4abb7f853dd",
                table: "sw_subscription",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_58f5c71eaab331645112cf8cfa5",
                table: "user",
                column: "avatarId",
                principalTable: "drive_file",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_afc64b53f8db3707ceb34eb28e2",
                table: "user",
                column: "bannerId",
                principalTable: "drive_file",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_3d6b372788ab01be58853003c93",
                table: "user_group",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_5cc8c468090e129857e9fecce5a",
                table: "user_group_invitation",
                column: "userGroupId",
                principalTable: "user_group",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_bfbc6305547539369fe73eb144a",
                table: "user_group_invitation",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_1039988afa3bf991185b277fe03",
                table: "user_group_invite",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_e10924607d058004304611a436a",
                table: "user_group_invite",
                column: "userGroupId",
                principalTable: "user_group",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_67dc758bc0566985d1b3d399865",
                table: "user_group_member",
                column: "userGroupId",
                principalTable: "user_group",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_f3a1b4bd0c7cabba958a0c0b231",
                table: "user_group_member",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_f4853eb41ab722fe05f81cedeb6",
                table: "user_keypair",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_b7fcefbdd1c18dce86687531f99",
                table: "user_list",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_605472305f26818cc93d1baaa74",
                table: "user_list_member",
                column: "userListId",
                principalTable: "user_list",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_d844bfc6f3f523a05189076efaa",
                table: "user_list_member",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_68881008f7c3588ad7ecae471cf",
                table: "user_note_pin",
                column: "noteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_bfbc6f79ba4007b4ce5097f08d6",
                table: "user_note_pin",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_51cb79b5555effaf7d69ba1cff9",
                table: "user_profile",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_6dc44f1ceb65b1e72bacef2ca27",
                table: "user_profile",
                column: "pinnedPageId",
                principalTable: "page",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_10c146e4b39b443ede016f6736d",
                table: "user_publickey",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ff9ca3b5f3ee3d0681367a9b447",
                table: "user_security_key",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_f272c8c8805969e6a6449c77b3c",
                table: "webhook",
                column: "userId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
