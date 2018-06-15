@PHONY: build

restore:
	@dotnet restore

build: restore
	@msbuild WakaTime.sln

