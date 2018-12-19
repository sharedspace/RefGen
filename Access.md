Private		0b0001						   // Accessible only by parent type
FamANDAssem	0b0010						   // Accessible by sub-types only in this Assembly
Family		0b0100						   // Accessible only by type and sub-types.
Assembly	0b0011	(Private | FamANDAssem)// Accessible by anyone in the Assembly.
FamORAssem	0b0101	(Private | Family)     // Accessible by sub-types anywhere, plus anyone in assembly.
Public		0b0110  (FamANDAssem 	| Family) // Accessible by anyone who has visibility to this scope.
