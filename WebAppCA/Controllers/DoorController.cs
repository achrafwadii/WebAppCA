using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppCA.Data;
using WebAppCA.Models;

public class DoorController : Controller
{
    private readonly ApplicationDbContext _context;

    public DoorController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Door/Index
    // Affiche la liste de toutes les portes avec leurs infos de base
    public async Task<IActionResult> Index()
    {
        var doors = await _context.Doors.ToListAsync();
        return View(doors); // Vue Index.cshtml (modèle : IEnumerable<DoorInfoModel>)
    }
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        // Charger les dispositifs depuis la base de données
        ViewBag.Devices = await _context.Devices.ToListAsync(); // <-- Assurez-vous que cette ligne existe

        var door = await _context.Doors.FindAsync(id);
        if (door == null) return NotFound();

        return View(door);
    }
    // POST: /Door/ToggleDoor - Verrouille/Déverrouille une porte
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleDoor(int doorID)
        {
            try
            {
                // Vérifier si la porte existe
                var door = await _context.Doors.FindAsync(doorID);
                if (door == null)
                {
                    TempData["Error"] = "Porte non trouvée.";
                    return RedirectToAction(nameof(Index));
                }

                // Récupérer ou créer le statut de la porte
                var doorStatus = await _context.DoorStatuses.FindAsync(doorID);
                if (doorStatus == null)
                {
                    doorStatus = new DoorStatusModel
                    {
                        DoorID = doorID,
                        IsOpen = false,
                        IsUnlocked = false,
                        HeldOpen = false,
                        AlarmFlags = 0
                    };
                    _context.DoorStatuses.Add(doorStatus);
                }

                // Inverser l'état de verrouillage
                doorStatus.IsUnlocked = !doorStatus.IsUnlocked;
                doorStatus.LastModified = DateTime.Now;

                await _context.SaveChangesAsync();

                string action = doorStatus.IsUnlocked ? "déverrouillée" : "verrouillée";
                TempData["Message"] = $"Porte '{door.Name}' {action} avec succès.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Erreur lors du changement d'état: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    // POST: /Door/Edit/5
    [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id , DoorInfoModel door)
{
    if (id != door.DoorID) return NotFound();

    if (ModelState.IsValid)
    {
        try
        {
            _context.Update(door);
            await _context.SaveChangesAsync();
            TempData["Message"] = "Porte modifiée avec succès";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Erreur lors de la mise à jour");
        }
    }
    
    // Recharger les dispositifs en cas d'erreur
    ViewBag.Devices = await _context.Devices.ToListAsync();
    return View(door);
}
    
    // GET: /Door/Create
    public async Task<IActionResult> Create()
    {
        // Récupérer les dispositifs depuis la base de données
        ViewBag.Devices = await _context.Devices.ToListAsync();
        return View();
    }



    // POST: /Door/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AddDoorModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                // Vérifier si le dispositif existe
                var deviceExists = await _context.Devices.AnyAsync(d => d.DeviceID == model.DeviceID);
                if (!deviceExists)
                {
                    ModelState.AddModelError("DeviceID", "Dispositif invalide.");
                    ViewBag.Devices = await _context.Devices.ToListAsync();
                    return View(model);
                }

                var newDoor = new DoorInfoModel
                {
                    Name = model.DoorName,
                    DeviceID = (int)model.DeviceID,
                    RelayPort = (byte)model.PortNumber,
                    Description = model.Description
                };

                _context.Add(newDoor);
                await _context.SaveChangesAsync();

                // Créer un statut initial "Verrouillée"
                var initialStatus = new DoorStatusModel
                {
                    DoorID = newDoor.DoorID,
                    IsLocked = true,
                    LastModified = DateTime.Now,
                    ChangeReason = "Création initiale"
                };
                _context.DoorStatuses.Add(initialStatus);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Porte ajoutée avec succès.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "Erreur de base de données : " + ex.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Erreur inattendue : " + ex.Message);
            }
        }

        ViewBag.Devices = await _context.Devices.ToListAsync();
        return View(model);
    }

    // GET: /Door/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var door = await _context.Doors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DoorID == id);
        if (door == null) return NotFound();
        return View(door); // Vue Delete.cshtml
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var door = await _context.Doors.FindAsync(id);
        if (door == null)
        {
            TempData["Error"] = "Porte introuvable.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            // Supprimer le statut associé (si existant)
            var status = await _context.DoorStatuses.FirstOrDefaultAsync(ds => ds.DoorID == id);
            if (status != null)
            {
                _context.DoorStatuses.Remove(status);
            }

            _context.Doors.Remove(door);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Porte supprimée avec succès.";
        }
        catch (DbUpdateException ex)
        {
            TempData["Error"] = "Impossible de supprimer : la porte est utilisée dans d'autres enregistrements.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Erreur inattendue : " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
    

}