# Волшебный Persistence под IExposable

## Как должно работать
Ты создаешь экземпляр IPersistenceContainer и в нем тыкаешь `Stream Save(IExposable root)` или `IExposable Load(Stream source)`
Преобразование происходит только между **Stream** и **IExposable**. Че там в **Stream** будет - вообще поебать, хоть чисто бинарные данные, хоть текст.