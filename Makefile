@PHONY: build

restore:
	@dotnet restore

build: restore
	@dotnet build

pack: build
	@dotnet pack