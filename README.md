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

#### Как публиковать пакеты в локальный фид
В Rider -> Правой кнопкой по проекту
-> Advanced Build Actions
-> Pack Selected Project
-> В консоли Build Output будет путь к файлу **.nupkg** `Successfully created package`
-> Этот файл ручками переноси в папку локального фида

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
- Возьми KludgeBox publishing API key из TickTick/AbroGames/Пароли
- `dotnet nuget push <Successfully created package.nupkg> --api-key <KludgeBox publishing API key> --source https://api.nuget.org/v3/index.json`

> [!NOTE]
> Тебе нужен именно файл `.nupkg`. 
> 
> `.snupkg` - файл с отладочными символами, но они по умолчанию зашиваются в основной файл

#### Дополнительные проверки после публикации в nuget.org
Это не обязательно, но желательно.
Скачай NuGet Package Explorer, найди в нем этот пакет, дождись, пока последняя версия проиндексируется и проверь,
чтобы все строки в разделе Health были с зеленым значком. Если есть желтые или красные значки - можешь
попытаться опубликовать 
