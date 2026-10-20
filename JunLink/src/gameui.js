// Optional "Game UI" look for the Jun Link pages. A button in the corner turns it on and off; the choice is remembered in this browser.
(function () {
  var KEY = 'mdrgUi';
  function get() { try { return localStorage.getItem(KEY); } catch (e) { return null; } }
  function set(v) { try { localStorage.setItem(KEY, v); } catch (e) { } }
  var link = document.createElement('link');
  link.rel = 'stylesheet'; link.href = '/gameui.css';
  document.head.appendChild(link);
  var on = get() === 'game';
  var btn = document.createElement('button');
  btn.id = 'gameuiBtn'; btn.type = 'button';
  btn.style.cssText = 'position:fixed;right:12px;bottom:12px;z-index:99999;cursor:pointer;font:700 12px Montserrat,Segoe UI,sans-serif;padding:8px 12px;border:0;color:#fff;background:#3f1f78;opacity:.92;border-left:4px solid #d3104f;border-radius:0';
  function apply() {
    document.documentElement.classList.toggle('gameui', on);
    btn.textContent = on ? 'Game UI: on' : 'Game UI: off';
  }
  btn.onclick = function () { on = !on; set(on ? 'game' : 'default'); apply(); };
  function add() { document.body.appendChild(btn); }
  if (document.body) add(); else document.addEventListener('DOMContentLoaded', add);
  apply();
})();
