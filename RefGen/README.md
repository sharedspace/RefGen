# RefGen - a metadata only Reference generator # 

## Commandline params 

	RefGen 
		-input <dll path>
		-output <dll path> (default = DirectoryOf($input)\ref\$FileNameOf($input))
		-accessModifierFilter public+protected+internal (default = all three)