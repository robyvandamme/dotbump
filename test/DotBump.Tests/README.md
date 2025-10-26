# dotBump Tests

## NuGet Tests

For the different NuGet test cases I have added example json result files to be able to easily test the logic to 
figure out the correct new release, if any.

### Data

The data is in `Data/NuGet`

#### Cases

There are a number of different cases:
* A PackageRegistration with inline package details with a single page of releases.  This 
  is the DotMarkdown example.
* A PackageRegistration with inline package details with a multiple page of releases.  This
  is the Moq example.
* A PackageRegistration with a list of Catalog links. The actual release info can be found in the Catalog pages. 
  The list of releases in the PackageRegistration contains a link with the start and end release version, for 
  instance "https://api.nuget.org/v3/registration5-semver1/gitversion.tool/page/4.0.1-beta1-36/5.0.0-beta2-25.json". 
  This is the GitVersion example.

##### DotMarkdown Example

The DotMarkdown example is a single json file `package-registration.json`, the registration index which also 
contains all the package release information. This is a single page of information.

##### GitVersion Example

The GitVersion example has:
* A `package-registration.json` which contains the release pages
* A `catalog-page.json` which contains the releases from `6.0.2` to `6.4.0`

##### Moq Example

The Moq example is a single json file `package-registration.json`, the registration index which also
contains all the package release information. This is 2 pages of information.
