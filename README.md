# Npm Publisher Support [![Github license](https://img.shields.io/github/license/vanifatovvlad/NpmPublisherSupport.svg)](#)  [![upm version](https://img.shields.io/github/package-json/v/vanifatovvlad/NpmPublisherSupport.svg)](#) [![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](#) [![Stars](https://img.shields.io/github/stars/vanifatovvlad/NpmPublisherSupport.svg?style=social)](https://github.com/vanifatovvlad/NpmPublisherSupport/stargazers) [![Watchers](https://img.shields.io/github/watchers/vanifatovvlad/NpmPublisherSupport.svg?style=social)](https://github.com/vanifatovvlad/NpmPublisherSupport/watchers)

A tool for managing Unity projects with multiple UnityPackageManager packages.
<br/>

[![Npm Publisher Support Preview](https://user-images.githubusercontent.com/26966368/57013385-f7dac980-6c13-11e9-9f2e-df4564603c1f.png)](#)

## About

Splitting up large codebases into separate independently versioned packages
is extremely useful for code sharing. However, making changes across many
repositories is messy and difficult to track, and testing across repositories
gets complicated really fast.

**NPM Publisher Suport is a tool that optimizes the workflow around managing multi-package
repositories.**

### Features

* Publish Npm packages to your registry directly from the Unity
* Track available updates for package dependencies:
  * From Npm registry
  * From Local project (it’s convenient if several packages are located in one project)
* Increment packages version from UnityEditor
  * Patch Dependent option allow to automatically patch dependant packages version (If there are several packages in the project that depend on the current one)

### What does a NPM Publisher Support repo look like?

There's actually very little to it. You have a file structure that looks like this:

```
Assets/
  Package-A/
    Sources/
      Runtime/
      package.json
      README.md
      LICENSE.md
  Package-B/
    Sources/
      Runtime/
      Editor/
      package.json
      README.md
      LICENSE.md
```

## Getting started
#### Step 1. Select registry
[![](https://user-images.githubusercontent.com/26966368/54922515-6643b200-4f19-11e9-912a-3b748c94e1f3.png)](#)
#### Step 2. Login
[![](https://user-images.githubusercontent.com/26966368/54920271-e1a26500-4f13-11e9-9040-12244318f78d.png)](#)
#### Step 3. Publish

### README Badge

Using NPM Publisher Support? Add a README badge to show it off: [![NPM Publisher Support](https://img.shields.io/badge/maintained%20with-NPM%20Publisher%20Support-blue.svg)](https://github.com/vanifatovvlad/NpmPublisherSupport)

```
[![NPM Publisher Support](https://img.shields.io/badge/maintained%20with-NPM%20Publisher%20Support-blue.svg)](https://github.com/vanifatovvlad/NpmPublisherSupport)
```

## Requirement
Unity 2019.1+

## License
MIT

## Authors
[Vanifatov Vlad](https://github.com/vanifatovvlad)
