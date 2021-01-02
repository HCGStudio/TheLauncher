#include <gtkmm.h>
#include <iostream>
#include <cstring>
#include <webkit2/webkit2.h>

bool startsWith(const char *pre, const char *str)
{
    size_t lenpre = strlen(pre),
            lenstr = strlen(str);
    return lenstr >= lenpre && memcmp(pre, str, lenpre) == 0;
}

void OnLoadChanged(WebKitWebView *widget, GdkEvent *event, gpointer data)
{
    if (startsWith("https://login.live.com/oauth20_desktop.srf", webkit_web_view_get_uri(widget)))
    {
        std::cout << webkit_web_view_get_uri(widget) << std::endl;
        exit(0);
    }
}

int main(int argc, char **argv)
{
    Glib::RefPtr<Gtk::Application> app = Gtk::Application::create(argc, argv, "");
    Gtk::Window window;
    window.set_default_size(800, 600);
    WebKitWebView *webView = WEBKIT_WEB_VIEW(webkit_web_view_new());
    window.add(*Glib::wrap(GTK_WIDGET(webView)));
    webkit_web_view_load_uri(webView, "https://login.live.com/oauth20_authorize.srf?"
                                  "client_id=00000000402b5328&response_type=code&"
                                  "scope=service%3A%3Auser.auth.xboxlive.com%3A%3AMBI_SSL&"
                                  "redirect_uri=https%3A%2F%2Flogin.live.com%2Foauth20_desktop.srf");
    window.show_all();
    g_signal_connect(webView, "load-changed", G_CALLBACK(OnLoadChanged), nullptr);
    app->run(window);
    exit(255);
}

