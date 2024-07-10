using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240709234348_MoveUserProfilePropsToSettingsStore")]
    public partial class MoveUserProfilePropsToSettingsStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
	        // First, create user_settings entries for all local users that don't have one yet
	        migrationBuilder.Sql($@"INSERT INTO ""user_settings"" (""userId"") (SELECT ""id"" FROM ""user"" WHERE ""host"" IS NULL) ON CONFLICT DO NOTHING;");

	        // Now, create the new columns
	        migrationBuilder.AddColumn<bool>(name: "alwaysMarkNsfw",
	                                         table: "user_settings",
	                                         type: "boolean",
	                                         nullable: false,
	                                         defaultValue: false);

	        migrationBuilder.AddColumn<bool>(name: "autoAcceptFollowed",
	                                         table: "user_settings",
	                                         type: "boolean",
	                                         nullable: false,
	                                         defaultValue: false);

	        migrationBuilder.AddColumn<string>(name: "email",
	                                           table: "user_settings",
	                                           type: "character varying(128)",
	                                           maxLength: 128,
	                                           nullable: true);

	        migrationBuilder.AddColumn<bool>(name: "emailVerified",
	                                         table: "user_settings",
	                                         type: "boolean",
	                                         nullable: false,
	                                         defaultValue: false);

	        migrationBuilder.AddColumn<string>(name: "password",
	                                           table: "user_settings",
	                                           type: "character varying(128)",
	                                           maxLength: 128,
	                                           nullable: true);

	        migrationBuilder.AddColumn<bool>(name: "twoFactorEnabled",
	                                         table: "user_settings",
	                                         type: "boolean",
	                                         nullable: false,
	                                         defaultValue: false);

	        migrationBuilder.AddColumn<string>(name: "twoFactorSecret",
	                                           table: "user_settings",
	                                           type: "character varying(128)",
	                                           maxLength: 128,
	                                           nullable: true);

	        migrationBuilder.AddColumn<string>(name: "twoFactorTempSecret",
	                                           table: "user_settings",
	                                           type: "character varying(128)",
	                                           maxLength: 128,
	                                           nullable: true);
	        
	        // Then, migrate the settings
	        migrationBuilder.Sql("""
	                             UPDATE "user_settings" s
	                             	SET "alwaysMarkNsfw" = p."alwaysMarkNsfw",
	                             		"autoAcceptFollowed" = p."autoAcceptFollowed",
	                             		"email" = p."email",
	                             		"emailVerified" = p."emailVerified",
	                             		"twoFactorTempSecret" = p."twoFactorTempSecret",
	                             		"twoFactorSecret" = p."twoFactorSecret",
	                             		"twoFactorEnabled" = p."twoFactorEnabled",
	                             		"password" = p."password"
	                             FROM "user_profile" p
	                             	WHERE p."userId" = s."userId";
	                             """);

	        // Finally, drop the old columns
	        migrationBuilder.DropColumn(name: "alwaysMarkNsfw",
	                                    table: "user_profile");

	        migrationBuilder.DropColumn(name: "autoAcceptFollowed",
	                                    table: "user_profile");
	        
	        migrationBuilder.DropColumn(name: "email",
	                                    table: "user_profile");

	        migrationBuilder.DropColumn(name: "emailVerified",
	                                    table: "user_profile");

	        migrationBuilder.DropColumn(name: "password",
	                                    table: "user_profile");

	        migrationBuilder.DropColumn(name: "twoFactorEnabled",
	                                    table: "user_profile");

	        migrationBuilder.DropColumn(name: "twoFactorSecret",
	                                    table: "user_profile");

	        migrationBuilder.DropColumn(name: "twoFactorTempSecret",
	                                    table: "user_profile");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
	        migrationBuilder.AddColumn<bool>(name: "alwaysMarkNsfw",
	                                         table: "user_profile",
	                                         type: "boolean",
	                                         nullable: false,
	                                         defaultValue: false);

	        migrationBuilder.AddColumn<bool>(name: "autoAcceptFollowed",
	                                         table: "user_profile",
	                                         type: "boolean",
	                                         nullable: false,
	                                         defaultValue: false);
	        
	        migrationBuilder.AddColumn<string>(name: "email",
	                                           table: "user_profile",
	                                           type: "character varying(128)",
	                                           maxLength: 128,
	                                           nullable: true,
	                                           comment: "The email address of the User.");

	        migrationBuilder.AddColumn<bool>(name: "emailVerified",
	                                         table: "user_profile",
	                                         type: "boolean",
	                                         nullable: false,
	                                         defaultValue: false);

	        migrationBuilder.AddColumn<string>(name: "password",
	                                           table: "user_profile",
	                                           type: "character varying(128)",
	                                           maxLength: 128,
	                                           nullable: true,
	                                           comment: "The password hash of the User. It will be null if the origin of the user is local.");

	        migrationBuilder.AddColumn<bool>(name: "twoFactorEnabled",
	                                         table: "user_profile",
	                                         type: "boolean",
	                                         nullable: false,
	                                         defaultValue: false);

	        migrationBuilder.AddColumn<string>(name: "twoFactorSecret",
	                                           table: "user_profile",
	                                           type: "character varying(128)",
	                                           maxLength: 128,
	                                           nullable: true);

	        migrationBuilder.AddColumn<string>(name: "twoFactorTempSecret",
	                                           table: "user_profile",
	                                           type: "character varying(128)",
	                                           maxLength: 128,
	                                           nullable: true);

	        migrationBuilder.Sql("""
	                             UPDATE "user_profile" p
	                             	SET "alwaysMarkNsfw" = s."alwaysMarkNsfw",
	                             		"autoAcceptFollowed" = s."autoAcceptFollowed",
	                             		"email" = s."email",
	                             		"emailVerified" = s."emailVerified",
	                             		"twoFactorTempSecret" = s."twoFactorTempSecret",
	                             		"twoFactorSecret" = s."twoFactorSecret",
	                             		"twoFactorEnabled" = s."twoFactorEnabled",
	                             		"password" = s."password"
	                             FROM "user_settings" s
	                             	WHERE s."userId" = p."userId";
	                             """);

	        migrationBuilder.DropColumn(name: "alwaysMarkNsfw",
	                                    table: "user_settings");

	        migrationBuilder.DropColumn(name: "autoAcceptFollowed",
	                                    table: "user_settings");

	        migrationBuilder.DropColumn(name: "email",
	                                    table: "user_settings");

	        migrationBuilder.DropColumn(name: "emailVerified",
	                                    table: "user_settings");

	        migrationBuilder.DropColumn(name: "password",
	                                    table: "user_settings");

	        migrationBuilder.DropColumn(name: "twoFactorEnabled",
	                                    table: "user_settings");

	        migrationBuilder.DropColumn(name: "twoFactorSecret",
	                                    table: "user_settings");

	        migrationBuilder.DropColumn(name: "twoFactorTempSecret",
	                                    table: "user_settings");
        }
    }
}
