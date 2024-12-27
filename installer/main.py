import io
import os
import sys
import zipfile
import requests
import threading
import configparser
import customtkinter
import CTkMessagebox as messagebox

OS = {
    "win32": "win",
    "linux2": "linux",
    "darwin": "macos",
}[sys.platform]
BEPINEX = f"725/BepInEx-Unity.IL2CPP-{OS}-x64-6.0.0-be.725%2Be1974e2"
POLYMOD = "https://github.com/PolyModdingTeam/PolyMod/releases/latest/download/PolyMod.dll"


def resource_path(path):
    try:
        base_path = sys._MEIPASS
    except Exception:
        base_path = os.path.abspath(".")
    return os.path.join(base_path, path)


def to_zip(request: requests.Response):
    return zipfile.ZipFile(io.BytesIO(request.content))


def browse():
    global path_entry
    path_entry.delete(0, customtkinter.END)
    path_entry.insert(0, customtkinter.filedialog.askdirectory())


def install():
    global progress_bar
    path = path_entry.get()
    try:
        if "Polytopia_Data" not in os.listdir(path):
            raise FileNotFoundError
    except FileNotFoundError:
        messagebox.CTkMessagebox(
            title="Error",
            message="The folder does not exist or is not valid!",
            icon="cancel",
            width=100,
            height=50
        )
        return
    path_entry.configure(state=customtkinter.DISABLED)
    browse_button.configure(state=customtkinter.DISABLED)
    disable_logs_button.configure(state=customtkinter.DISABLED)
    install_button.pack_forget()
    progress_bar = customtkinter.CTkProgressBar(app, determinate_speed=50 / 2)
    progress_bar.grid(column=0, row=2, columnspan=2, padx=5, pady=5)
    progress_bar.set(0)
    threading.Thread(target=_install, daemon=True, args=(path, )).start()


def _install(path):
    to_zip(
        requests.get(
            f"https://builds.bepinex.dev/projects/bepinex_be/{BEPINEX}.zip"
        )
    ).extractall(path)
    progress_bar.step()

    open(path + "/BepInEx/plugins/PolyMod.dll", "wb").write(
        requests.get(POLYMOD).content
    )
    os.makedirs(path + "/BepInEx/config/")
    config_file = open(path + "/BepInEx/config/BepInEx.cfg", "a+")
    config = configparser.ConfigParser()
    config.read_file(config_file)
    logs = not bool(disable_logs_button.get())
    if "Logging.Console" in config:
        config["Logging.Console"]["Enabled"] = logs
    else:
        config["Logging.Console"] = {
            "Enabled": logs}
    config.write(config_file)
    progress_bar.step()

    customtkinter.CTkButton(app, text="Launch", command=launch).grid(
        column=0, row=3, columnspan=2, padx=5, pady=5
    )


def launch():
    os.system("start steam://rungameid/874390")
    app.destroy()
    sys.exit()


app = customtkinter.CTk()
app.title("PolyMod")
app.iconbitmap(default=resource_path("icon.ico"))
app.resizable(False, False)

path_entry = customtkinter.CTkEntry(
    app, placeholder_text="Game path", width=228)
browse_button = customtkinter.CTkButton(
    app, text="Browse", command=browse, width=1)
disable_logs_button = customtkinter.CTkCheckBox(app, text="Disable logs")
disable_logs_button.select()
install_button = customtkinter.CTkButton(app, text="Install", command=install)

path_entry.grid(column=0, row=0, padx=5, pady=5)
browse_button.grid(column=1, row=0, padx=(0, 5), pady=5)
disable_logs_button.grid(column=0, row=1, columnspan=2, padx=5, pady=5)
install_button.grid(column=0, row=2, columnspan=2, padx=5, pady=5)

app.mainloop()
