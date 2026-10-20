# New menu and UI art (approved by the owner on 2026-10-20)

Only these items from `fix/ART-FIX-028` were taken. Everything else in that branch (custard cream cake, banana flambée cake, redrawn logo, title background, redrawn price board, weather particles) was rejected or is still undecided and is NOT in `main`.

## Drinks (art only, no recipe yet)
| Display name | Source of the real item | Files |
|---|---|---|
| Trà chanh dây | Owner photo `IMG_5213` (passionfruit tea, colour `#EDA417`) | `Art/Models/Food/SM_Drink_PassionfruitTea.obj`, `Art/Materials/Food/MAT_Drink_PassionfruitTea.mat`, `Prefabs/Items/NewMenu/PF_Item_Drink_PassionfruitTea.prefab` |
| Trà ô long chanh tươi | Owner photo `IMG_5214` (amber lime tea). Owner chose this name on 2026-10-20; Anti first called it "honey lemon tea" | `Art/Models/Food/SM_Drink_OolongFreshLemonTea.obj`, `Art/Materials/Food/MAT_Drink_OolongFreshLemonTea.mat`, `Prefabs/Items/NewMenu/PF_Item_Drink_OolongFreshLemonTea.prefab` |
Shared texture: `Art/Textures/Food/T_NewDrinks_BaseColor.png`.
Note: the mesh inside `SM_Drink_OolongFreshLemonTea.obj` still carries the old internal object name (`SM_Drink_HoneyLemonTea`); it can be renamed the next time the generator runs. The cup frame and accessories (dome lid, straw, ice, lemon slices) were modelled by Anti from the photos; there is no photo of those exact details.

## Icons (the originals drawn by Anti, taken as they are; owner chose "use the original icons")
24 PNG files, 256x256, in `Resources/UI/Icons/` (loaded at run time with `Resources.Load`): ingredients (Ice, Jelly, Batter, Wrap), upgrades (BiggerIceBin, Pantry, StringLights, WholesaleDeal, GoldenMenuBoard), state (Star_Full, Star_Empty, Money, Patience, Goal), nights (Rain, Hot, PowerCut, Festival, Inspector, Clear), customers (Student, Family, Office, Regular). There is no owner source for them; they are generic UI symbols made for the game screens.

## Not taken
The cakes, `LOGO_TramChanh`, `BG_Title`, `T_MenuBoard_Prices`: no real source or redrawn text. The real logo is `IMG_5205`, the real price board is `IMG_4932 2`; use those files, never a redrawn version.

## How they are wired (2026-10-20)
- Icons are loaded from `Resources/UI/Icons` (`UiIcons`): shop rows, shop stars and night icon, end-of-day stars, and the customer icon in the order list.
- The two drinks are order labels of kind Drink: `drink.chanh_day` (2 stars) and `drink.o_long_chanh_tuoi` (3 stars), defined in `MenuBook`; their names are in `SO_PromptText_TramChanhMain.asset`. The prefabs `PF_Item_Drink_*` are not placed anywhere yet. Making a different tea bag per flavour (its own bag on the rack, its own cup colour) is later art and station work.
