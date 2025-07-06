### How to publish a new version of Quick SQL

----

#### Increase the version number
- Open `quick-sql.csproj` and change the `<AssemblyVersion>` tag to the new version number.

#### Publish the project
- Right-click on the `quick-sql` project in Visual Studio and select `Publish`.
- In Settings click on `Show all` and forward to `Settings`.
- Increase the `Publish Version` to the new version number.
- Click on `Finish` and then `Publish`.

#### Create a new tag version in GitHub
- Push the changes to the `main` branch of the Quick SQL repository.
- Open a terminal and navigate to the Quick SQL repository.
- Run the following commands to create and push a new tag:
- `git tag v<version>`
- `git push origin --tags`

#### Create a Self-Extracting .exe using 7zip
- Compress all the published files and folders as a zip file.
- Rename it as `QuickSQL-v<version>.zip` where `<version>` is the new version number.

#### Create a new release on GitHub
- Go to the [Quick SQL GitHub releases page](https://github.com/fellipeborges/quick-sql/releases)
- Click on `Draft a new release`.
- Set the tag version to `v<version>` where `<version>` is the new version number.
- Set the title to `Quick SQL <version>` where `<version>` is the new version number.
- Add a description of the changes in this version.
- Upload the `QuickSQL-v<version>.zip` file.
- Click on `Publish release`.
