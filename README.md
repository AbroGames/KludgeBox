# KludgeBox
Class library for gamedev in Godot/C#

## Разработка
При разработке используется несколько подходов к доставке пакета в тестовое окружение:
- Локальный фид - для совсем уж трешовых изменений, экспериментов и тестов, которые скорее всего не пойдут в публичную версию
- nuget.org - для всего остального, просто не забудь выставить `PrereleaseFlag` в `KludgeBox.csproj` 

### Локальный NuGet Feed
#### Как создать
Локальный фид - это просто папка. Создай папку в удобном месте и пропиши в терминал.
```shell
dotnet nuget add source "C:\Path\To\LocalNuGetFeed" --name LocalFeed
```
Можно в качестве KludgeBoxLocalFeed указать папку внутри этого проекта, куда происходит билд **.nupkg** файлов

#### Как публиковать пакеты в локальный фид
В Rider -> Правой кнопкой по проекту
-> Advanced Build Actions
-> Pack Selected Project
-> В консоли Build Output будет путь к файлу **.nupkg** `Successfully created package`
-> Этот файл ручками переноси в папку локального фида

Можно билдить сразу в нужную папку: `dotnet pack --output "C:\Path\To\LocalNuGetFeed" -p:IncludeSymbols=false -p:IncludeSource=false`  
Или находясь в другом проекте: `dotnet pack "C:\Path\To\KludgeBox\KludgeBox\KludgeBox.csproj" --output "C:\Path\To\LocalNuGetFeed" -p:IncludeSymbols=false -p:IncludeSource=false`  
Опционально можно добавить в конец предыдущей команды для обновления кеша зависимого от KludgeBox проекта:  
`; Remove-Item -Path "$env:USERPROFILE\.nuget\packages\kludgebox" -Recurse -Force; dotnet restore --no-cache`

#### Как найти локальные пакеты
В Rider 
- NuGet Tool (по идее оно где-то внизу слева) 
- Рядом со строкой поиска и Solution выбрать All feeds
- Начать искать пакет в поисковой строке по названию
- В правой части рядом с выбором версии в выпадающем списке вместо nuget.org выбираем LocalFeed (или что ты там указал после `--name`)
- Установи этот пакетище, бахнув на плюсик

### Публикация в nuget.org
В Rider
- Правой кнопкой по проекту
- Advanced Build Actions
- Pack Selected Project
- В консоли Build Output будет путь к файлу **.nupkg** `Successfully created package`, скопируй его
- Возьми KludgeBox publishing API key из хранилища паролей
- `dotnet nuget push <Successfully created package.nupkg> --api-key <KludgeBox publishing API key> --source https://api.nuget.org/v3/index.json`
- Ошибку связанную с загрузкой **.snupkg** можно игнорировать, главное, чтобы в консоли было `Your package was pushed.`

> [!NOTE]
> Тебе нужен именно файл `.nupkg`. 
> 
> `.snupkg` - файл с отладочными символами, но они по умолчанию зашиваются в основной файл

#### Дополнительные проверки после публикации в nuget.org
Это не обязательно, но желательно.  
Зайди в [NuGet Package Explorer](https://nuget.info/packages/KludgeBox/), дождись, пока последняя версия проиндексируется и проверь,
чтобы все строки в разделе Health были с зеленым значком. Если есть желтые или красные значки - можешь
попытаться опубликовать файл .snupkg
