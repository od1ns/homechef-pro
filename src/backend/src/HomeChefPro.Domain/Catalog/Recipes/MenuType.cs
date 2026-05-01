using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Catalog.Recipes;

public enum MenuType
{
    [DbValue("fixed")]          Fixed,
    [DbValue("daily_special")]  DailySpecial,
}
