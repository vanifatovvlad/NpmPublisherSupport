# Npm Package Loader [![NPM Publisher Support](https://img.shields.io/badge/maintained%20with-NPM%20Publisher%20Support-blue.svg)](https://github.com/vanifatovvlad/NpmPublisherSupport)

Npm предлагает удобный способ управления пакетами. Однако некоторые пакеты 
обладают слишком большим размером и не могут быть загружены в репозиторий,
либо требуют установки непосредственно в папку Assets проекта.

Npm Package Loader позволяет архивировать часть ассетов в стандартный
`unitypackage` пакет, который может быть размещён как в самом npm,
так и на ftp сервере и будет автоматически скачан и распакован
непосредственно в проект.

## Создание пакета

#### 1. Установить Npm Package Loader и [Npm Publisher Support](https://github.com/vanifatovvlad/NpmPublisherSupport)
Могут быть установлены из npm репозитория

#### 2. Создать package.json

Может выглядеть примерно так:
```
{
  "name": "com.greenbuttongames.npm-package-loader-demo-sdk",
  "displayName": "Npm Package Loader DEMO",
  "description": "Do not use",
  "version": "0.1.0",
  "unity": "2019.1",
  "author": "Vanifatov Vlad (https://github.com/vanifatovvlad)",
  "dependencies": {}
}
```

#### 3. Создать ассет UnityPackage Loader
[![Create asset](https://user-images.githubusercontent.com/26966368/62519337-04367a00-b834-11e9-9279-327948c65fa0.png)](#)

Ассет должен располагаться рядом с `package.json`

#### 4. Указать данные
[![Asset content](https://user-images.githubusercontent.com/26966368/62519851-0fd67080-b835-11e9-9cd0-d018c4a6bfc7.png)](#)

В `Packed Assets` можно добавлять как отдельные файлы, так и целые папки. 

#### 5. Добавить зависимость

После создания Loader в окне `Npm Publish` появится раздел `External loaders`, 
где необходимо добавить в зависимости `npm-package-loader` с помощью кнопки `Add` 
и опубликовать пакет, после чего он может быть установлен из npm репозитория.

> Если зависимость от `npm-package-loader` отмечена как Unknown 
> необходимо проверить что установлен пакет Npm Package Loader 
> после чего вручную указать в `package.json` актуальную версию пакета

[![Install deps](https://user-images.githubusercontent.com/26966368/62523535-a35f6f80-b83c-11e9-9504-677e40907eca.png)](#)

## Установка пакета

Созданный пакет может быть скачан из npm репозитория. После установки пакета
автоматически должно появиться окно подтверждения скачивания дополнительных файлов

[![Install package](https://user-images.githubusercontent.com/26966368/62521665-c0923f00-b838-11e9-805a-f6fd1920bf2a.png)](#)

> Если окно не появилось, оно может быть вызвано вручную в меню `Window/Check Npm Package Loaders`

После подтверждения дополнительные файлы будут скачаны и распакованы

[![Import package](https://user-images.githubusercontent.com/26966368/62522173-cccacc00-b839-11e9-9052-3a99a31370aa.png)](#)

## FAQ

#### Для чего создается папка Assets/Packages?
Это служебная папка необходимая для отслеживания установленных пакетов

#### Как обновить пакет до новой версии?
Новую версию можно скачать через npm. После обновления npm пакета должно атоматически появиться предложение обновить дополнительные файлы

#### Как переустановить пакет?
Для переустановки можно удалить подпапку ассета из Assets/Packages и выполнить команду `Window/Check Npm Package Loaders`
