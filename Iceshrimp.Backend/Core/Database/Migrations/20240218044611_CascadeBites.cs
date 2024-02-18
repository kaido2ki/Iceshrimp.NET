using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iceshrimp.Backend.Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class CascadeBites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bite_bite_targetBiteId",
                table: "bite");

            migrationBuilder.DropForeignKey(
                name: "FK_bite_note_targetNoteId",
                table: "bite");

            migrationBuilder.DropForeignKey(
                name: "FK_bite_user_targetUserId",
                table: "bite");

            migrationBuilder.AddForeignKey(
                name: "FK_bite_bite_targetBiteId",
                table: "bite",
                column: "targetBiteId",
                principalTable: "bite",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_bite_note_targetNoteId",
                table: "bite",
                column: "targetNoteId",
                principalTable: "note",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_bite_user_targetUserId",
                table: "bite",
                column: "targetUserId",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bite_bite_targetBiteId",
                table: "bite");

            migrationBuilder.DropForeignKey(
                name: "FK_bite_note_targetNoteId",
                table: "bite");

            migrationBuilder.DropForeignKey(
                name: "FK_bite_user_targetUserId",
                table: "bite");

            migrationBuilder.AddForeignKey(
                name: "FK_bite_bite_targetBiteId",
                table: "bite",
                column: "targetBiteId",
                principalTable: "bite",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_bite_note_targetNoteId",
                table: "bite",
                column: "targetNoteId",
                principalTable: "note",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_bite_user_targetUserId",
                table: "bite",
                column: "targetUserId",
                principalTable: "user",
                principalColumn: "id");
        }
    }
}
