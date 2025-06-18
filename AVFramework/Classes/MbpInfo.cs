using System;

if (subtrackDetails.cParas > 0)
{
    try
    {
        // Проверяем, не превышает ли размер массива разумные пределы
        if (subtrackDetails.cParas > 1000000) // Ограничиваем максимальный размер
        {
            throw new Exception("Слишком большое количество параграфов для обработки");
        }

        arrayParaDesc = new PTS.FSPARADESCRIPTION[subtrackDetails.cParas];
        // ... existing code ...
    }
    catch (OutOfMemoryException ex)
    {
        // Обработка ошибки нехватки памяти
        Console.WriteLine($"Ошибка памяти при создании массива параграфов: {ex.Message}");
        return; // Прерываем обработку текущего элемента
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка при обработке параграфов: {ex.Message}");
        return;
    }
}
// ... existing code ... 