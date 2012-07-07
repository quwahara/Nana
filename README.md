# Nana プログラミング言語

## はじめに

Nana プログラミング言語は .NET Framework で動くプログラミング言語です。
Nana は「左から右に代入したい！」を野望に開発を始めました。
そのころはまだ **R言語** の存在を知りませんでした。
ほかに文法で目指しているところは
* なるべく一人でメンテナンスできるように、コンパイラの実装が単純であること
* なるべくタイプしないでいいように済むような文法であること
です。

完成度は低く、まだ簡単な文法しかビルドできません。
エラー処理なども、まだろくに実装できていません。
とりあえず少し動くようになりましたという感じです。

## 実行環境

Nana を使うには

Windows XP 32bit
Microsoft.NET Framework v2.0

が必要です。
多分、v2.0 以上の .NET が入っていれば動くと思います。

## 動かし方

とりあえず c:\tmp ディレクトリがあるとして、
そこに次の3つのファイルをダウンロードします。

https://github.com/quwahara/Nana/raw/master/Nana/bin/Release/Nana.exe
https://github.com/quwahara/Nana/raw/master/Nana/bin/Release/NanaLib.dll
https://github.com/quwahara/Nana/raw/master/Nana/bin/Release/HelloWorld.nana

cmd.exe を開き、c:tmp の下に移動し、次のように入力、リターンします。

```
C:\Tmp>Nana.exe HelloWorld.nana
```

うまくいくと次のようなメッセージが出ます。

```
Microsoft (R) .NET Framework IL Assembler.  Version 2.0.50727.3053
Copyright (c) Microsoft Corporation.  All rights reserved.
Assembling 'HelloWorld.il'  to EXE --> 'HelloWorld.exe'
Source file is ANSI

Assembled global method .cctor
Assembled global method 0
Creating PE file

Emitting classes:

Emitting fields and methods:
Global  Methods: 2;

Emitting events and properties:
Global
Writing PE file
Operation completed successfully



C:\Tmp>

```

そして同じディレクトリに  HelloWorld.exe ができていので
それを実行すると下のように出ます。

```
C:\Tmp>HelloWorld.exe
Hello, world!

```
もしでたら、おめでとうございます！
あなたの環境で立派に動きました。


うまくいかないときは、次のことを試してみて下さい。
Nanaでは ilasm.exe というプログラムを実行しています。
これは通常、下のディレクトリに配置されていてます。
Nana では規定でそこに ilasm.exe があることを期待しています。
```
C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727\ilasm.exe
```


環境変数 NANA_ILASM_PATH で次のように直接 ilasm.exe の場所を指定できます。
お使いの環境で ilasm.exe があるところを確認して指定して下さい。

```
c:\tmp> set NANA_ILASM_PATH=C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727\ilasm.exe
```
or
```
c:\tmp> set NANA_ILASM_PATH=C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319\ilasm.exe
```


## ライセンス

MIT ライセンスで公開しています。 
ライセンスの全文は下で参照できます。

https://raw.github.com/quwahara/Nana/master/LICENSE

