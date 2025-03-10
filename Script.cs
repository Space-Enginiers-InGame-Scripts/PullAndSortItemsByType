
string lcd_name = "DEBUG_LCD";

string black_listed_name = "SYS_SORTING_CACHE";

string[] container_prefixes = {
  "[INGOTS]",
  "[ICE]",
  "[STUFF]",
  "[COMPONENTS]"
};

bool DO_PULL_FROM_OTHER_CONAINERS = true;



IMyTextSurface debug_lcd;
Dictionary<string, List<IMyTerminalBlock>> Containers;
List<IMyAssembler> Assemblers;
List<IMyRefinery> Refineries;

List<IMyCargoContainer> all_other_containers;
List<IMyCargoContainer> TEMP_all_other_containers;

public Program() {
  debug_lcd = GridTerminalSystem.GetBlockWithName(lcd_name) as IMyTextSurface;

  Containers = new Dictionary<string, List<IMyTerminalBlock>>();
  Assemblers = new List<IMyAssembler>();
  Refineries = new List<IMyRefinery>();
  all_other_containers = new List<IMyCargoContainer>();

  GridTerminalSystem.GetBlocksOfType<IMyAssembler>(Assemblers);
  GridTerminalSystem.GetBlocksOfType<IMyRefinery>(Refineries);
  

  Runtime.UpdateFrequency = UpdateFrequency.Update100; // Update every tick
}

public void Save() {}

public void Main(string argument, UpdateType updateSource) {
  TEMP_all_other_containers = new List<IMyCargoContainer>();
  GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(TEMP_all_other_containers);
  string debug_text = "";
  debug_text += $"Found {TEMP_all_other_containers.Count()} other containers\n====\n";
  
  foreach (string container_prefix in container_prefixes) {
    if (debug_lcd != null) {
      debug_text += $"Working on prefix: {container_prefix}\n";
    }
    Containers[container_prefix] = new List<IMyTerminalBlock>();
    GridTerminalSystem.SearchBlocksOfName(container_prefix, Containers[container_prefix]);
    debug_text += $"Found {Containers[container_prefix].Count()} containers\n===============\n";
  }

  foreach (IMyCargoContainer other_container in TEMP_all_other_containers) {
    bool containsAnyPrefix = container_prefixes.Any(prefix => other_container.CustomName.Contains(prefix));
    if ((other_container.CustomName != black_listed_name && !containsAnyPrefix)) {
      debug_text += $"Adding {other_container.CustomName} to other containers\n";
      all_other_containers.Add(other_container);
    }
  }
  
  if (DO_PULL_FROM_OTHER_CONAINERS) {
    pullFromOtherContainers();
  }
  sortContainerItems();
  sortAssemberItems();
  sortRefineryItems();
  if (debug_lcd != null) {
    debug_lcd.WriteText(debug_text);
  }
}

// public void moveItemsFromCargo(string container_key, IMyCargoContainer container_object) {

// }

public string doSortingAndMoving(List<MyInventoryItem> items, IMyTerminalBlock container) {
  string debug_text = "";
  foreach (MyInventoryItem item in items) {
    string itemFullType = item.Type.ToString();
    int firstStringPosition = itemFullType.IndexOf("_");
    int secondStringPosition = itemFullType.IndexOf("/");
    string itemType = itemFullType.Substring(firstStringPosition+1, secondStringPosition - firstStringPosition-1);
    if (itemFullType.IndexOf("Ice") != -1) itemType = "Ice";

    debug_text += item + "\n";
    debug_text += $"Item type: {itemType}\n";
    if (itemType == "Ingot") {
      if (Containers["[INGOTS]"].Count != 0){
        foreach (IMyTerminalBlock container_dist in Containers["[INGOTS]"]) {
          if (!container_dist.GetInventory(0).IsFull) {
            container.GetInventory(0).TransferItemTo(container_dist.GetInventory(0), item);
            break;
          }
        }
      }
    } else if (itemType == "Ice") {
      if (Containers["[ICE]"].Count != 0){
        foreach (IMyTerminalBlock container_dist in Containers["[ICE]"]) {
          if (!container_dist.GetInventory(0).IsFull) {
            container.GetInventory(0).TransferItemTo(container_dist.GetInventory(0), item);
            break;
          }
        }
      }
    } else if (itemType == "Component") {
      if (Containers["[COMPONENTS]"].Count != 0){
        foreach (IMyTerminalBlock container_dist in Containers["[COMPONENTS]"]) {
          if (!container_dist.GetInventory(0).IsFull) {
            container.GetInventory(0).TransferItemTo(container_dist.GetInventory(0), item);
            break;
          }
        }
      }
    } else {
      if (Containers["[STUFF]"].Count != 0){
        foreach (IMyTerminalBlock container_dist in Containers["[STUFF]"]) {
          if (!container_dist.GetInventory(0).IsFull) {
            container.GetInventory(0).TransferItemTo(container_dist.GetInventory(0), item);
            break;
          }
        }
      }
    }
  }
  return debug_text;
}

public void pullFromOtherContainers() {
  string debug_text = "";
  foreach (IMyTerminalBlock container in all_other_containers) {
    if (!(container.GetInventory(0).ItemCount > 0)) { continue; }
    List<MyInventoryItem> items = new List<MyInventoryItem>();
    container.GetInventory(0).GetItems(items);
    debug_text += doSortingAndMoving(items, container);
  }
  if (debug_lcd != null) {
    debug_lcd.WriteText(debug_text);
  }
}

public void sortContainerItems(){
  string debug_text = "START MOVING!\n\n";
  foreach (string container_prefix in container_prefixes) {
    foreach (IMyTerminalBlock container in Containers[container_prefix]) {
      if (!(container.GetInventory(0).ItemCount > 0)) { continue; }
      if (container.CustomName == black_listed_name) { continue; }
      debug_text +=  $"Container: {container_prefix}\n";
      List<MyInventoryItem> items = new List<MyInventoryItem>();
      container.GetInventory(0).GetItems(items);
      debug_text += doSortingAndMoving(items, container);
      debug_text += "=====================\n";
    }
  }
  if (debug_lcd != null) {
    debug_lcd.WriteText(debug_text, false);
    // throw new Exception("STOP");
  }
}

public void sortRefineryItems() {
  if (Refineries.Count > 0) {
    foreach (IMyRefinery refinery in Refineries) {
      if (refinery.GetInventory(1).ItemCount > 0) {
        List<MyInventoryItem> items = new List<MyInventoryItem>();
        refinery.GetInventory(1).GetItems(items);

        if (Containers["[INGOTS]"].Count == 0) {
          break;
        }
        foreach (MyInventoryItem item in items) {
          foreach (IMyCargoContainer container_dist in Containers["[INGOTS]"]) {
            if (!container_dist.GetInventory(0).IsFull) {
              refinery.GetInventory(1).TransferItemTo(container_dist.GetInventory(0), item);
              break;
            }
          }
        }
      }
    }
  }
}

public void sortAssemberItems() {
  if (Assemblers.Count > 0) {
    foreach (IMyAssembler assembler in Assemblers) {
      if(assembler.GetInventory(1).ItemCount > 0) {
        List<MyInventoryItem> items = new List<MyInventoryItem>();
        assembler.GetInventory(1).GetItems(items);

        if (Containers["[COMPONENTS]"].Count == 0) {
          break;
        }
        foreach (MyInventoryItem item in items) {
          foreach (IMyCargoContainer container_dist in Containers["[COMPONENTS]"]) {
            if (!container_dist.GetInventory(0).IsFull) {
              assembler.GetInventory(1).TransferItemTo(container_dist.GetInventory(0), item);
              break;
            }
          }
        }
      }
    }
  }
}
