![GitHub top language](https://img.shields.io/github/languages/top/we-kode/mml.media?label=c%23&logo=dotnet&style=for-the-badge) ![GitHub Release Date](https://img.shields.io/github/release-date/we-kode/mml.media?label=Last%20release&style=for-the-badge) ![Docker Image Version (latest by date)](https://img.shields.io/docker/v/w3kod3/wekode.mml.media?logo=docker&style=for-the-badge) ![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/we-kode/mml.media/docker-image.yml?label=Docker%20CI&logo=github&style=for-the-badge) ![GitHub](https://img.shields.io/github/license/we-kode/mml.media?style=for-the-badge)

# Media

Media is part of the [My Media Lib](https://we-kode.github.io/mml.project/) project. This service is responsible for managing [records](https://we-kode.github.io/mml.project/concepts/records). Tasks of this service are to manage the uploads, compression, indexing, streaming and downloading of records.

## Local Development

Setup your local development environment to run [.NET](https://learn.microsoft.com/en-us/dotnet/) and [docker](https://docs.docker.com/).

Clone this repository and configure it, like described in the following sections. Consider that this service does not run standalone. You have to [setup the backend](https://we-kode.github.io/mml.project/setup/backend#configuration-3) to run the [My Media Lib](https://we-kode.github.io/mml.project/) project.

### Appsettings

A template of the appsettings can be found at [./Media.API/default.appsettings.json](./Media.API/default.appsettings.json). Create a local copy and rename it how you like. Fill in the configuration. [Check](https://we-kode.github.io/mml.project/setup/backend#configuration-3) the official documentation on how to fill in the documentation.

### Configure .env

Create a local copy of the `.env` file name e.g. `dev.env` and fill in the configuration. Check the [official documentation](https://we-kode.github.io/mml.project/setup/backend) on how to configure the `.env` file.

### Local build the docker image

To build the docker image on your machien run

```
docker-compose --env-file dev.env up --build -d
```

## Deployment
### Releases

New releases will be available if new features or improvements exists. Check the corresponding release to learn what has changed. Binary releases are only available as docker images on [docker hub](https://hub.docker.com/r/w3kod3/wekode.mml.media).

### Setup

Check the official documentation on [how to setup](https://we-kode.github.io/mml.project/setup/backend#configure-media-service) the media service.

## Contribution

Please check the official documentation on [how to contribute](https://we-kode.github.io/mml.project/contribution).
