   M S B A M L   `   `   `  ����  alesya-rassylka     alesya_rassylka.MainWindow   5      W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   T PMahApps.Metro, Version=2.0.0.0, Culture=neutral, PublicKeyToken=51482d6f650b2b3f;Controls,http://metro.mahapps.com/winfx/xaml/controls  5   	     � Title$  Рассылка��5   	   $	��750��6   $
��1000��6'    � WindowStartupLocation$ CenterScreen=�64   ���#DFE3EB � WindowStyle$ SingleBorderWindow=�5   	    � Icon$  /icons8-почта-100.png��6*    � 	Resources 5	         FileContextMenu  FileItemStyle �� 
TargetType �� Property �� Value �� Triggers	 P� Property
 P� Value  ActionButton �� 
TargetType �� CornerRadius �� Padding �� TextAlignment �� Triggers  	NavButton �� Orientation ��Name ��Name �� 
TargetName �� ElementName  SmallNavButton �� BasedOn  
MenuButton �� ScaleX �� ScaleY  FormatButton��%�  &	        &	 �     &	 �    &	 <    &	 �    7  &	 	    &	     �� 5
   
   .@�5      m� -   5        $��Удалить��5      6(   �� 5      m� 5      -   5        $��Открыть файл��5      6-   m� 5      -   5        $$��Показать в папке��5      61   5      �� 5   
   " ��6&   .�5      ��  � ��
 ���� 6+    5      P� 5      	 �@J�IsMouseOver$

 True��61   . �5      ��  � �� �#E0E6F863   5      5      �� 5   
   " ��6%   .�5      ��  � ��
 �����6+   �� 5       � �� �#172A746+   �� 5       � ��$ 24(�6)   �� 5       � ��$ Arial Black)�6+   �� 5       � ��$
 Bold"�6+   �� 5        � ��$
 10,5q�6(   �� 5!       � ��$ 1q�60   �� 5"       � �� �#172A746,   �� 5#       �@�Cursor$
 Hand��6'   �� 5$       � ��$ 30��6*   �� 5%       � �� 5&      �� 5'      " ��6&   .R�5(      �� #��z��6!   #��z��5)   !   #��z��5*   !   $ 5��5+   !   # z��5,   !   ._�5-      �� $��Center=�6/   $��Center=�6L   $ Center=�5.   /   5/       50      P� 51      	 �@J�IsMouseOver$

 True��6=   . �52   "   ��  � �� �#E0E6F86?   53      P� 54      	 � ��$

 True��6;   . �55   "   ��  � �� �#C0D0F06?   56      58      5:      5;      �� 5=   
   " ��6"   .�5>      ��  � ��
 ���� 6+   �� 5?       � �� �#172A746+   �� 5@       � ��$ 24(�6)   �� 5A       � ��$ 5q�6(   �� 5B       � ��$ Arial Black)�6+   �� 5C       � ��$ Black"�6+   �� 5D       � ��$ 0q�60   �� 5E       �@�Cursor$
 Hand��6'   �� 5F       � �� 5G      �� 5H      " ��6&   .R�5I      �� $ Vertical=�6%   $��Center=�6<   .+�5J      �� $ TextBlockContent��6(   #��z��5K   (   #��z��5L   (   #��z��5M   (   $��Center=�5N   (   �� 5O      $ 	Underline��6%   $��2��68   #��z��6C   $��0��5P   %   $��Center=�6/   5Q       5R      P� 5S      	 �@J�IsMouseOver$

 True��6=   . �5T   "   �� $ 	Underline��6)    6Q   �� *ActualWidth+$ TextBlockContent�� � ��5U      5W      5Y      5Z      �� 5\   
   " ��6'   8   .�5]      ��  � ��$ 24(�6)   �� 5^       � ��$ 3q�6(   5_      �� 5a   
   " ��6#   .�5b      ��  � ��
 ���� 6+   �� 5c       � ��
 �����6+   �� 5d       � ��$ 18(�6)   �� 5e       � ��$ Arial Black)�6+   �� 5f       � ��$ 10q�6(   �� 5g       � ��$ Stretch=�64   �� 5h       � ��$ 0q�60   �� 5i       �@�Cursor$
 Hand��6'   �� 5j       �@J�RenderTransformOrigin$ 0.5,0.5&�66   �� 5k       �@��TextAlignment$ Center=�68   �� 5l       � ��$ 72��6'   �� 5m       � �� 5n      �� 5o      " ��6&   .R�5p      �� #��z��6!   # z��5q   !   $��Stretch=�5r   !   ._�5s      �� #��z��6(   #��z��5t   (   #��z��5u   (   $
 Left=�5v   (   $
��Wrap=�5w   (   5x       5y      P� 5z      	 �@J�IsMouseOver$

 True��6=   . �5{   "   ��  � �� �#3050A56?   5|      P� 5}      	 � ��$

 True��6;   . �5~   "   ��  � z� 5   &   �� 5�   *   $
 0.95Q�69   $
 0.95Q�6G   5�   #   5�      5�      5�      5�      �� 5�   
   " ��6%   .�5�      ��  � ��
 ���� 6+   �� 5�       � ��$ 0q�60   �� 5�       � �� �#172A746+   �� 5�       � ��$ 14(�6)   �� 5�       � ��$ 30��6&   �� 5�       � ��$ 30��6'   �� 5�       �@�Cursor$
 Hand��6'   �� 5�       � �� 5�      �� 5�      " ��6&   .R�5�      �� #��z��6!   $ 3��5�   !   ._�5�      �� $��Center=�6/   $��Center=�6L   5�      5�      5�       5�      P� 5�      	 �@J�IsMouseOver$

 True��61   . �5�      ��  � �� �#E0E6F863   5�      5�      5�      .��5�      �  � ColumnDefinitions 5�   
   �� 5�      $	��280 �6   �� 5�      $��20 �6   �� 5�      $��* �6   .+�5�   
   �� ���#172A74$��0��6&   
������ $��0,0,1,1q�5�   +   $ 	0,15,15,0��5�      ._�5�      ]� .+�5�      �� .+�5�      �� 
�������$ 15��60   $��10q�6B   $ 10q�6N   $��53��6[   ._�5�      �� $��
ПОЧТА��6$   $��36(�61   $
��Bold"�6?   ���#172A74$��Center=�5�   $   $��Center=�6?   $	��147��6\   $��Arial Black)�6h   5�      �� 5�      -   5        $)��#Добавить категорию��5�      #��[ 6:   $��0,0,0,0q�5�   7   $��45��6H   �� 5�      $��Hidden=�6#   $	��580��6H   .��5�      �� ��6'   �� *TemplateCategories+��5�      �� 5�   "   .R�5�   &   �� -   5        #��[ 5�   F   ��6-   �� *Name+
 � Tag 5�   -   ��  � ContextMenu 5�   *   �� 5�   .   .@�5�   2   m� -   5        $'��!Удалить категорию��5�   ;   6V   5�   /   5�   '   5�   #   5�      5�      5�      5�      5�      � 5�   
   $��
0,10,22,20q�6   $��2��6#    � RowDefinitions 5�      �� 5�      $
��Auto �6    �� 5�      $
��Auto �6    �� 5�      $
��Auto �6    �� 5�      $
��Auto �6    �� 5�      $
��Auto �6    �� 5�      $��* �6    �� 5�      $
��Auto �6    .+�5�      �� $ 
Horizontal=�6   $
��Left=�62   $
��0,10q�6M   $��38��6[   $��0��6g   .+�5�      �� -   5        $��Настройки��5�      #��[ 6-   $��0,0,20,0q�6P   6b   �� 5�      -   5        $��Справка��5�      #��[ 6+   $��0,0,20,0q�6N   6`   �� 5�      -	   5        $��О программе��5�      #��[ 6/   6R   5�      �� 5�      $��1��6   ���#172A74$��1q�68   $ 5��6L   
�������$��0,0,0,0q�6p   ._�5�      �  5�      �� 5�      $
��Auto �6(   �� 5�      $��* �6(   �� 5�      $��40 �6(   .+�5�      �� -
   5         ��Name$ SubjectTextBox��5�      $��0��66   $��	10,5,10,0q�6C   $��	Тема:��5�   
   $��0,0,0,1q�5�   
   ���#172A74
������ $��14(�5�   
   $	��0,5q�6   5�   
   �� 5�      -   5         ��Name$ MessageRichTextBox��5�   "   $��1��6>   $	��250��6K   
�������$��0q�6k   $��6,10,0,0q�6    5�      ��%X   'J�       �� 5      " J�6$   .�5  "   ��  �@��Margin$ 0q�6;   5     5     ��5     �� 5     " ��6.   .R�5  "   �� #��z��6)   #��z��6S   #��z��5     $ 5��6Q   ._�5  &   ��  ��Name$ PART_ContentHost��63   5	  #   5
     5     �� 5     $��2��6   ���#F5F7FA���#E0E3E8$��0,1,0,0q�6U   $ 0,0,5,5��6o   ._�5     �� $ 
Horizontal=�6%   $
��10,0q�6>   $��Center=�6L   .+�5     �� -   5        $��B��5  %   #��[ 5     $��32��6?   $��32��6J   $
��Bold"�5      � ToolTip$ Жирный��6+   $��14(�6<   �� 5     -   5        $��I��5  %   #��[ 5     $��32��6?   $��32��6J   $��Italic$�5     $ Курсив��6,   $��14(�6=   $��5,0,0,0q�6K   �� 5     -   5        #��[ 5     $��32��6?   $��32��6J   $ Подчеркнутый��5     $��14(�60   $��5,0,0,0q�6>   .��5  "   �� $��U��6,   $��	Underline{�65   5     �� 5     $��1��6(   $��20��62   ���#172A74$��	10,0,10,0q�6M   �� 5!     -   5        #��[ 5"  	   $��32��6/   $��32��6:   $' !Вставить картинку��5#  	   .��5$  "   �� $��14(�6,   🖼6>   5%     5&     5'     5(     5)     � 50     $��0,25,0,0q�6   $��2��6%    51     �� 52     $
��Auto �6'   �� 53     $��* �6'   .+�57     �� -   5        $#��Прикрепить файл��57     #��[ 63   $
��Left=�58     $��0,0,0,0q�64   6E   �� 5;     
����������#172A74$��1q�6B   $ 5��5<     $ 5q�6*   $��Stretch=�5=     $��1��67   $��10,0,0,0q�6G   ._�5>     �� $
��Auto=�6#   $��Disabled=�5?  #   $
��Auto��5@  #   $��50��61   .��5A     �� -   5          ��Name$  AttachedFilesList��5A  '   ��5B     �� 5C  "   .R�5D  &   �� $ 
Horizontal=�61   # [  5E  1   #��[ 5F  1   .+�5G  *   �� ! �� Padding$! 5q�6N   ��64   �� *
FileName+6N   5H  '   5I  #   5K     5L     5M     5N     � 5Q     $��0,25,0,0q�6   $��3��6%    5R     �� 5S     $��104.775 �6'   �� 5T     $��117.419 �6'   �� 5U     $��* �6'   .+�5Y     �� -   5        $��Отправитель��5Y     #��[ 6/   $
��Left=�5Z     $��2��6O   �� 5]     
����������#172A74$��1q�6B   $ 5��5^     $ 2q�6*   $��2��5_     $��30,0,0,0q�6)   ._�5`     �� -   5        $ SenderTextBox��5`     
������ $��0q�6N   $��5q�6b   $��Center=�5a     $��Stretch=�6@   " �� 
IsReadOnly$
" True��6^   5b     5c     � 5f     $��0,25,0,0q�6   $��4��6%    5g     �� 5h     $
��Auto �6'   �� 5i     $��* �6'   .+�5l     �� -   5        $ ��Получатель(ли)��5l     #��[ 62   $
��Left=�5m     $��0,0,10,0q�64   6F   �� 5o     
����������#172A74$��1q�6B   $ 5��5p     $ 5q�6*   $��Stretch=�5q     $��1��67   $��30,0,0,0q�6G   ._�5r     �� $
��Auto=�6#   $��Disabled=�5s  #   $
��Auto��5t  #   $��60��61   .��5u     �� -   5        $  RecipientList��5u  '   ��5v     �� 5w  "   .R�5x  &   �� $��Segoe UI)�60   $! 5q�6W   ��6F   �� 6W   5y  #   5{     5|     5}     5~     �� 5�     $ 
Horizontal=�6   $
��Left=�62   $��0,10,0,0q�6M   $��6��6_   .+�5�     �� -   5        $%��Отправить письмо��5�     #��[ 64   6Z   �� 5�     -   5        $��Отменить��5�     #��[ 6,   $��20,0,0,0q�6R   6d   5�     5�     5�     5�     