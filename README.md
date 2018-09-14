# HiddenDesktopViewer

![window](https://user-images.githubusercontent.com/43233361/45525547-68141680-b80e-11e8-8650-52a2117be911.PNG)


This tool reveals hidden desktops and investigate processes/threads utilizing hidden desktops.

This tool is useful for the research such as the following threats.
 - Ransomware and ScreenLocker which create hidden desktops
 - Online banking malware using Hidden VNC (hVNC)
 
 
このツールは隠されたデスクトップを明らかにし、隠されたデスクトップを利用するプロセスやスレッドを調査するツールです。
 
隠されたデスクトップを作成するランサムウェアやスクリーンロッカー、
また、Hidden VNC(hVNC)を利用するオンラインバンキングマルウェアなどの調査に有用です。


![default](https://user-images.githubusercontent.com/43233361/45525567-867a1200-b80e-11e8-8575-d1976ec31896.jpg)



If there are some processes with information with the different desktop, this tool can display in different colors as shown below.

もし異なるデスクトップの情報を持つプロセスがある場合、以下のように色分けされて表示されます。


![2](https://user-images.githubusercontent.com/43233361/45525580-9691f180-b80e-11e8-8455-fef5e53e9013.jpg)


Pink: It is operating on a desktop which is not the default desktop (has a handle)
Orange: It has a window created/generated from another desktop that is not currently active
Blue: It has the specified desktop information that is not the default in the process informaion(PEB).
Gray: Operating in session 0 (service etc.)


ピンク：デフォルトデスクトップではないデスクトップで動作している（ハンドルを持っている）
オレンジ：現在アクティブではない別のデスクトップから作成・生成されたウインドウを持っている
ブルー：起動時点でデフォルトではないデスクトップを指定されている
グレー：セッション０で動作している（サービス等）



※ We are not responsible for any problems arising when using this tool, so please use only those who can understand that point.

※なお、本ツールを利用した際に発生したいかなる問題についても一切の責任を負いかねますのでその点ご理解いただける方のみご自由にご利用ください。
