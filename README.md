# HiddenDesktopViewer

This tool reveals hidden desktops and investigate processes/threads utilizing hidden desktops.   
このツールは隠されたデスクトップを明らかにし、隠されたデスクトップを利用するプロセスやスレッドを調査するツールです。　

  ※For detailed backgrounds and explanations about the threat, please see the blog post below (Japanese only).<br>
  ※脅威についての詳しい背景や解説は以下のブログ記事をご覧ください。<br>
   https://www.mbsd.jp/blog/20180914.html

![window](https://user-images.githubusercontent.com/43233361/45525547-68141680-b80e-11e8-8650-52a2117be911.PNG)



This tool is useful for the research such as the following threats.
 - Ransomware and ScreenLocker which create hidden desktops
 - Trojan such as the online banking malware(gozi/dreambot/ursnif/ramnit/carberp/etc..) using Hidden VNC (hVNC)
 
 
 
隠されたデスクトップを作成するランサムウェアやスクリーンロッカー、
また、Hidden VNC(hVNC)を利用するオンラインバンキングマルウェアなどの調査に有用です。　　　



- The compiled binary is below.   
- コンパイル済バイナリは以下です。   
https://github.com/AgigoNoTana/HiddenDesktopViewer/blob/master/HiddenDesktopViewer_Bin.zip


(*HiddenDesktopViewer requires Microsoft . NET Framework 4.5 or higher.  ( 4.0 can be used, but an error occurs during working.)
（※「HiddenDesktopViewer」の正常動作にはMicrosoft . NET Framework 4.5以上が必要です。(4.0で起動はできますが動作中にエラーが出ます)）

　　　
![default](https://user-images.githubusercontent.com/43233361/45525567-867a1200-b80e-11e8-8575-d1976ec31896.jpg)



If there are some processes with information with the different desktop, this tool can display in different colors as shown below.

もし異なるデスクトップの情報を持つプロセスがある場合、以下のように色分けされて表示されます。


![2](https://user-images.githubusercontent.com/43233361/45525580-9691f180-b80e-11e8-8455-fef5e53e9013.jpg)

Pink: It is operating on a desktop which is not the default desktop (has a handle)  
Orange: It has a window created/generated from another desktop that is not currently active  
Blue: It has the specified desktop information that is not the default in the process informaion(PEB).  
Gray: Operating in session 0 (service etc.) 


ピンク：デフォルトデスクトップではないデスクトップで動作している(ハンドルを持っている)   
オレンジ：現在アクティブではない別のデスクトップから作成・生成されたウインドウを持っている   
ブルー：起動時点でデフォルトではないデスクトップを指定されている   
グレー：セッション０で動作している(サービス等)   
　　　

[HOW TO USE]

Please watch these video to know how to use the HiddenDesktopViewer.   
使用方法は以下の動画をご覧ください。

(Video-1): HiddenDesktopViewer VS Tyrant Ransomware.
https://www.mbsd.jp/blog/img/20180914_HiddenDesktopViewer%20-%20Tyrant%20case%20-.mp4

(Video-2): HiddenDesktopViewer VS HiddenVNC used by some online banking malware(gozi/dreambot/ursnif/ramnit/carberp/etc..) .
https://www.mbsd.jp/blog/img/20180914_HiddenDesktopViewer%20-case%20of%20HiddenVNC-.mp4


<br>--------------------<br>
   
※ We are not responsible for any problems arising when using this tool, so please use only those who can understand that point.

※なお、本ツールを利用した際に発生したいかなる問題についても一切の責任を負いかねますのでその点ご理解いただける方のみご自由にご利用ください。
