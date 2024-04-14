#!/bin/bash
set -e

if [[ $(uname) == "Darwin" ]]; then
	SED="gsed"
else
	SED="sed"
fi

import="using Microsoft.EntityFrameworkCore.Infrastructure;"
dbc="    [DbContext(typeof(DatabaseContext))]"

for file in $(find "$(dirname $0)/Migrations" -name '*.Designer.cs'); do
	echo "$file"
	csfile="${file%.Designer.cs}.cs"
	if [[ ! -f $csfile ]]; then
		echo "$csfile doesn't exist, exiting"
		exit 1
	fi
	lineno=$($SED -n '/^{/=' "$csfile")
	((lineno+=2))
	migr=$(grep "\[Migration" "$file")
	$SED -i "${lineno}i \\$migr" "$csfile"
	$SED -i "${lineno}i \\$dbc" "$csfile"
	$SED -i "2i $import" "$csfile"
	rm "$file"
done
