game 'rdr3'
fx_version 'adamant'
rdr3_warning 'I acknowledge that this is a prerelease build of RedM, and I am aware my resources *will* become incompatible once RedM ships.'

client_scripts {
    'client/json.lua',
    'client/client.lua'
  }
  
  server_scripts {
    'server/server.lua'
  }
  
  shared_scripts {
    'locale.lua',
    'config.lua',
    'locales/en.lua',
    'locales/fr.lua'
  }

ui_page "html/index.html"

files {
  "html/index.html",
  "html/styles.css",
  "html/reset.css",
  "html/jquery.min.js",
  "html/listener.js"
}